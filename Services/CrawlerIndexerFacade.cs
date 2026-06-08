using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using rag_can_aspx.Services.Jobs;

namespace rag_can_aspx.Services
{
    /// <summary>
    /// Resultado de una accion de control (iniciar/parar).
    /// </summary>
    public class JobActionResult
    {
        public bool Accepted { get; set; }
        public string Message { get; set; }

        public static JobActionResult Ok(string message)
        {
            return new JobActionResult { Accepted = true, Message = message };
        }

        public static JobActionResult Fail(string message)
        {
            return new JobActionResult { Accepted = false, Message = message };
        }
    }

    /// <summary>
    /// Ultimas lineas de los logs de crawler e indexer.
    /// </summary>
    public class LogsSnapshot
    {
        public List<string> Crawler { get; set; } = new List<string>();
        public List<string> Indexer { get; set; } = new List<string>();
    }

    /// <summary>
    /// CAPA DE CONTROL (NIVEL 1). Punto unico de entrada para iniciar/parar/consultar el
    /// crawler y el indexer. La consumen las paginas .aspx, el scheduler y el servicio WCF.
    /// No ejecuta trabajo largo en el hilo del request: lanza jobs en segundo plano con
    /// HostingEnvironment.QueueBackgroundWorkItem y persiste el estado via JobStatusManager.
    /// </summary>
    public class CrawlerIndexerFacade
    {
        // -------- CRAWL --------

        public JobActionResult StartCrawl()
        {
            return StartCrawlInternal(null, DefaultMaxPages(), DefaultMaxDepth());
        }

        public JobActionResult StartCrawl(int maxPages, int maxDepth)
        {
            return StartCrawlInternal(null, maxPages, maxDepth);
        }

        public JobActionResult StartCrawlSource(string url)
        {
            return StartCrawlSource(url, DefaultMaxPages(), DefaultMaxDepth());
        }

        public JobActionResult StartCrawlSource(string url, int maxPages, int maxDepth)
        {
            if (string.IsNullOrWhiteSpace(url))
                return JobActionResult.Fail("URL vacia.");

            Uri parsed;
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out parsed))
                return JobActionResult.Fail("La URL introducida no es valida.");

            return StartCrawlInternal(new List<string> { parsed.ToString() }, maxPages, maxDepth);
        }

        public JobActionResult StopCrawl()
        {
            bool ok = JobStatusManager.RequestStopCrawl();
            return ok
                ? JobActionResult.Ok("Solicitud de parada enviada al crawler.")
                : JobActionResult.Fail("No hay ningun crawl en ejecucion.");
        }

        public JobRunStatus GetCrawlStatus()
        {
            return JobStatusManager.ReadCrawlStatus();
        }

        public JobRunStatus GetLastCrawlRun()
        {
            // El fichero de estado persiste la ultima ejecucion (o la actual si esta corriendo).
            return JobStatusManager.ReadCrawlStatus();
        }

        private JobActionResult StartCrawlInternal(List<string> seeds, int maxPages, int maxDepth)
        {
            CancellationToken token;
            if (!JobStatusManager.TryBeginCrawl(out token))
                return JobActionResult.Fail("Ya hay un crawl en ejecucion.");

            try
            {
                if (seeds == null)
                {
                    seeds = CargarSemillas();
                }

                if (seeds.Count == 0)
                {
                    JobStatusManager.EndCrawl();
                    return JobActionResult.Fail("No hay URLs validas para procesar.");
                }

                List<string> seedsLocal = seeds;
                int pages = maxPages, depth = maxDepth;

                HostingEnvironment.QueueBackgroundWorkItem(async hostToken =>
                {
                    using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, hostToken))
                    {
                        try
                        {
                            await CrawlJob.RunAsync(seedsLocal, pages, depth, linked.Token).ConfigureAwait(false);
                        }
                        finally
                        {
                            JobStatusManager.EndCrawl();
                        }
                    }
                });

                return JobActionResult.Ok("Crawl iniciado en segundo plano.");
            }
            catch (Exception ex)
            {
                JobStatusManager.EndCrawl();
                return JobActionResult.Fail("No se pudo iniciar el crawl: " + ex.GetBaseException().Message);
            }
        }

        // -------- INDEX --------

        public JobActionResult StartIndexing()
        {
            CancellationToken token;
            if (!JobStatusManager.TryBeginIndex(out token))
                return JobActionResult.Fail("Ya hay una indexacion en ejecucion.");

            try
            {
                HostingEnvironment.QueueBackgroundWorkItem(async hostToken =>
                {
                    using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, hostToken))
                    {
                        try
                        {
                            await IndexJob.RunAsync(linked.Token).ConfigureAwait(false);
                        }
                        finally
                        {
                            JobStatusManager.EndIndex();
                        }
                    }
                });

                return JobActionResult.Ok("Indexacion iniciada en segundo plano.");
            }
            catch (Exception ex)
            {
                JobStatusManager.EndIndex();
                return JobActionResult.Fail("No se pudo iniciar la indexacion: " + ex.GetBaseException().Message);
            }
        }

        public JobActionResult StopIndexing()
        {
            bool ok = JobStatusManager.RequestStopIndex();
            return ok
                ? JobActionResult.Ok("Solicitud de parada enviada al indexer.")
                : JobActionResult.Fail("No hay ninguna indexacion en ejecucion.");
        }

        public JobRunStatus GetIndexingStatus()
        {
            return JobStatusManager.ReadIndexStatus();
        }

        public JobRunStatus GetLastIndexingRun()
        {
            return JobStatusManager.ReadIndexStatus();
        }

        // -------- FUENTES / LOGS --------

        public List<SourceStatus> GetSources()
        {
            SourcesStatusFile file = JobStatusManager.ReadSources();
            return file != null && file.Sources != null ? file.Sources : new List<SourceStatus>();
        }

        public SourceStatus GetSourceStatus(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            return GetSources().FirstOrDefault(s =>
                string.Equals(s.Url, url, StringComparison.OrdinalIgnoreCase));
        }

        public LogsSnapshot GetLogs(int lines)
        {
            if (lines <= 0)
                lines = 50;
            if (lines > 2000)
                lines = 2000;

            return new LogsSnapshot
            {
                Crawler = JobLogger.Tail(JobLogger.CrawlerLog, lines),
                Indexer = JobLogger.Tail(JobLogger.IndexerLog, lines)
            };
        }

        // -------- SCHEDULER (ciclo crawl -> index secuencial) --------

        /// <summary>
        /// Ejecuta, en un unico work item de fondo, primero el crawl y despues la indexacion,
        /// para que el indexado recoja lo que el crawl acaba de marcar como needs_index.
        /// Usado por el Scheduler interno.
        /// </summary>
        public JobActionResult StartScheduledCycle(bool runCrawl, bool runIndex)
        {
            if (JobStatusManager.IsCrawlRunning || JobStatusManager.IsIndexRunning)
                return JobActionResult.Fail("Hay un job en ejecucion; se omite el ciclo programado.");

            int pages = DefaultMaxPages(), depth = DefaultMaxDepth();

            HostingEnvironment.QueueBackgroundWorkItem(async hostToken =>
            {
                if (runCrawl && !hostToken.IsCancellationRequested)
                {
                    CancellationToken token;
                    if (JobStatusManager.TryBeginCrawl(out token))
                    {
                        using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, hostToken))
                        {
                            try
                            {
                                List<string> seeds = CargarSemillasSafe();
                                if (seeds.Count > 0)
                                    await CrawlJob.RunAsync(seeds, pages, depth, linked.Token).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                JobLogger.Append(JobLogger.CrawlerLog, "[scheduler] Error en crawl: " + ex.GetBaseException().Message);
                            }
                            finally
                            {
                                JobStatusManager.EndCrawl();
                            }
                        }
                    }
                }

                if (runIndex && !hostToken.IsCancellationRequested)
                {
                    CancellationToken token2;
                    if (JobStatusManager.TryBeginIndex(out token2))
                    {
                        using (var linked2 = CancellationTokenSource.CreateLinkedTokenSource(token2, hostToken))
                        {
                            try
                            {
                                await IndexJob.RunAsync(linked2.Token).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                JobLogger.Append(JobLogger.IndexerLog, "[scheduler] Error en index: " + ex.GetBaseException().Message);
                            }
                            finally
                            {
                                JobStatusManager.EndIndex();
                            }
                        }
                    }
                }
            });

            return JobActionResult.Ok("Ciclo programado iniciado.");
        }

        // -------- helpers --------

        private List<string> CargarSemillas()
        {
            CrawlerSettings settings = CrawlerSettings.Load();
            var provider = new SeedUrlProvider(settings);
            SeedLoadResult result = provider.Load();
            return result.Urls ?? new List<string>();
        }

        private List<string> CargarSemillasSafe()
        {
            try
            {
                return CargarSemillas();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static int DefaultMaxPages()
        {
            return ReadInt("Crawler:MaxPages", 50, 1, 10000);
        }

        private static int DefaultMaxDepth()
        {
            return ReadInt("Crawler:MaxDepth", 2, 0, 10);
        }

        private static int ReadInt(string key, int def, int min, int max)
        {
            string raw = ConfigurationManager.AppSettings[key];
            int value;
            if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out value) && value >= min && value <= max)
                return value;
            return def;
        }
    }
}
