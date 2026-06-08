using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace rag_can_aspx.Services.Jobs
{
    /// <summary>
    /// Job de crawling incremental. Orquesta el CrawlerService existente (que ya descarga,
    /// limpia y escribe los .txt + sidecars *.metadata.json) y, tras cada dominio, calcula
    /// que paginas son nuevas/cambiadas (needs_index=true) frente a las que no cambiaron.
    /// Persiste el progreso en App_Data/status y escribe logs en crawler.log.
    /// </summary>
    public static class CrawlJob
    {
        private sealed class DomainScan
        {
            public int Total;
            public int Changed;
            public int Skipped;
            public string Title;
            public string Sha256;
            public string TxtPath;
            public string MetadataPath;
        }

        public static async Task RunAsync(List<string> seeds, int maxPages, int maxDepth, CancellationToken cancellationToken)
        {
            seeds = (seeds ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            string projectRoot = JobStatusManager.ProjectRoot;
            string appData = JobStatusManager.AppDataDir;
            CrawlerSettings settings = CrawlerSettings.Load();
            string baseFolder = PathHelper.ResolverRutaCarpeta(appData, null); // App_Data/crawlings/
            var crawler = new CrawlerService(settings, projectRoot);

            var status = new JobRunStatus
            {
                Kind = "crawl",
                RunId = Guid.NewGuid().ToString("N"),
                State = JobStates.Running,
                StartedAt = DateTime.UtcNow.ToString("o"),
                TotalSources = seeds.Count,
                ProgressPercent = 0
            };
            JobStatusManager.WriteCrawlStatus(status);
            JobLogger.Append(JobLogger.CrawlerLog, string.Format("=== Crawl iniciado: {0} fuentes (maxPages={1}, maxDepth={2}) ===", seeds.Count, maxPages, maxDepth));

            SourcesStatusFile sourcesFile = JobStatusManager.ReadSources();
            object gate = new object();
            int done = 0;

            var semaphore = new SemaphoreSlim(Math.Max(1, settings.MaxConcurrentDomains));

            async Task ProcessSeedAsync(string seed)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    lock (gate)
                    {
                        status.CurrentUrl = seed;
                        JobStatusManager.WriteCrawlStatus(status);
                    }

                    Uri uri;
                    if (!Uri.TryCreate(seed, UriKind.Absolute, out uri))
                    {
                        RegistrarFuente(sourcesFile, gate, new SourceStatus
                        {
                            Url = seed,
                            Host = seed,
                            State = "failed",
                            LastError = "URL invalida",
                            LastCrawledAt = DateTime.UtcNow.ToString("o")
                        });
                        lock (gate) { status.FailedSources++; }
                        JobLogger.Append(JobLogger.CrawlerLog, "ERROR URL invalida: " + seed);
                        return;
                    }

                    string domainFolder = Path.Combine(baseFolder, crawler.GenerarNombreCarpetaDominio(uri));

                    JobLogger.Append(JobLogger.CrawlerLog, "Crawling " + seed);
                    CrawlerService.ResultadoCrawl resultado = await crawler
                        .CrawlDominioAsync(seed, domainFolder, maxPages, maxDepth, cancellationToken)
                        .ConfigureAwait(false);

                    DomainScan scan = EscanearDominio(domainFolder);

                    var source = new SourceStatus
                    {
                        Url = seed,
                        Host = uri.Host,
                        LastCrawledAt = DateTime.UtcNow.ToString("o"),
                        HttpStatus = resultado.Exitoso ? 200 : 0,
                        Title = scan.Title,
                        ContentSha256 = scan.Sha256,
                        TxtPath = scan.TxtPath,
                        MetadataPath = scan.MetadataPath,
                        NeedsIndex = scan.Changed > 0,
                        PagesTotal = scan.Total,
                        PagesChanged = scan.Changed,
                        PagesSkipped = scan.Skipped,
                        LastError = resultado.Exitoso ? null : resultado.Mensaje
                    };

                    if (!resultado.Exitoso)
                    {
                        bool wasCancelled = cancellationToken.IsCancellationRequested;
                        source.State = wasCancelled ? "stopped" : "failed";
                        if (!wasCancelled)
                            lock (gate) { status.FailedSources++; }
                        JobLogger.Append(JobLogger.CrawlerLog, string.Format(
                            wasCancelled ? "DETENIDO {0}: {1}" : "FALLO {0}: {1}", seed, resultado.Mensaje));
                    }
                    else if (scan.Changed == 0 && scan.Total > 0)
                    {
                        source.State = "skipped";
                        lock (gate) { status.SkippedSources++; }
                        JobLogger.Append(JobLogger.CrawlerLog, string.Format("SIN CAMBIOS {0}: {1} paginas (hash sin cambios)", seed, scan.Total));
                    }
                    else
                    {
                        source.State = "ok";
                        lock (gate) { status.ProcessedSources++; }
                        JobLogger.Append(JobLogger.CrawlerLog, string.Format("OK {0}: {1} paginas, {2} nuevas/cambiadas", seed, scan.Total, scan.Changed));
                    }

                    RegistrarFuente(sourcesFile, gate, source);
                }
                catch (OperationCanceledException)
                {
                    RegistrarFuente(sourcesFile, gate, new SourceStatus
                    {
                        Url = seed,
                        Host = TryHost(seed),
                        State = "failed",
                        LastError = "Cancelado por el usuario",
                        LastCrawledAt = DateTime.UtcNow.ToString("o")
                    });
                    JobLogger.Append(JobLogger.CrawlerLog, "CANCELADO " + seed);
                }
                catch (Exception ex)
                {
                    RegistrarFuente(sourcesFile, gate, new SourceStatus
                    {
                        Url = seed,
                        Host = TryHost(seed),
                        State = "failed",
                        LastError = ex.GetBaseException().Message,
                        LastCrawledAt = DateTime.UtcNow.ToString("o")
                    });
                    lock (gate) { status.FailedSources++; }
                    JobLogger.Append(JobLogger.CrawlerLog, string.Format("EXCEPCION {0}: {1}", seed, ex.GetBaseException().Message));
                }
                finally
                {
                    lock (gate)
                    {
                        done++;
                        status.ProgressPercent = seeds.Count == 0 ? 100 : (int)Math.Round(done * 100.0 / seeds.Count);
                        JobStatusManager.WriteCrawlStatus(status);
                    }
                    semaphore.Release();
                }
            }

            try
            {
                await Task.WhenAll(seeds.Select(ProcessSeedAsync)).ConfigureAwait(false);

                status.State = cancellationToken.IsCancellationRequested ? JobStates.Stopped : JobStates.Completed;
            }
            catch (OperationCanceledException)
            {
                status.State = JobStates.Stopped;
            }
            catch (Exception ex)
            {
                status.State = JobStates.Error;
                status.LastError = ex.GetBaseException().Message;
            }
            finally
            {
                semaphore.Dispose();
                status.CurrentUrl = null;
                status.ProgressPercent = 100;
                status.FinishedAt = DateTime.UtcNow.ToString("o");
                JobStatusManager.WriteCrawlStatus(status);
                JobLogger.Append(JobLogger.CrawlerLog, string.Format(
                    "=== Crawl {0}: {1} ok, {2} sin cambios, {3} con fallo ===",
                    status.State, status.ProcessedSources, status.SkippedSources, status.FailedSources));
            }
        }

        private static void RegistrarFuente(SourcesStatusFile file, object gate, SourceStatus source)
        {
            lock (gate)
            {
                int idx = file.Sources.FindIndex(s =>
                    string.Equals(s.Url, source.Url, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                    file.Sources[idx] = source;
                else
                    file.Sources.Add(source);

                JobStatusManager.WriteSources(file);
            }
        }

        private static string TryHost(string seed)
        {
            Uri uri;
            return Uri.TryCreate(seed, UriKind.Absolute, out uri) ? uri.Host : seed;
        }

        /// <summary>
        /// Lee los sidecars *.metadata.json del dominio para contabilizar paginas nuevas/cambiadas
        /// (needs_index=true) frente a las que no cambiaron, y extraer datos de la pagina "home".
        /// </summary>
        private static DomainScan EscanearDominio(string domainFolder)
        {
            var scan = new DomainScan();
            if (string.IsNullOrWhiteSpace(domainFolder) || !Directory.Exists(domainFolder))
                return scan;

            int minPage = int.MaxValue;

            foreach (string path in Directory.GetFiles(domainFolder, "*.metadata.json", SearchOption.TopDirectoryOnly))
            {
                PageMetadataDocument doc;
                try
                {
                    string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                    doc = JsonConvert.DeserializeObject<PageMetadataDocument>(json);
                }
                catch
                {
                    continue;
                }

                if (doc == null || doc.PageMetadata == null)
                    continue;

                scan.Total++;
                if (doc.PageMetadata.NeedsIndex)
                    scan.Changed++;
                else
                    scan.Skipped++;

                if (doc.PageMetadata.PageNumber > 0 && doc.PageMetadata.PageNumber < minPage)
                {
                    minPage = doc.PageMetadata.PageNumber;
                    scan.Title = doc.PageMetadata.Title;
                    scan.Sha256 = doc.PageMetadata.Sha256;
                    scan.TxtPath = doc.PageMetadata.File;
                    scan.MetadataPath = ToSidecarRel(doc.PageMetadata.File);
                }
            }

            return scan;
        }

        private static string ToSidecarRel(string txtRelPath)
        {
            if (string.IsNullOrWhiteSpace(txtRelPath))
                return null;
            if (txtRelPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                return txtRelPath.Substring(0, txtRelPath.Length - 4) + ".metadata.json";
            return txtRelPath + ".metadata.json";
        }
    }
}
