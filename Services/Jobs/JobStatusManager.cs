using System;
using System.IO;
using System.Threading;
using System.Web.Hosting;

namespace rag_can_aspx.Services.Jobs
{
    /// <summary>
    /// Estado central de los jobs de crawl/index. Es thread-safe y persiste a disco
    /// (App_Data/status) para que el progreso sobreviva a la navegacion del usuario y a
    /// recargas de pagina. Tambien implementa el "single-flight" (impide ejecuciones
    /// concurrentes duplicadas) y guarda el CancellationTokenSource vivo para poder parar.
    /// </summary>
    public static class JobStatusManager
    {
        private static readonly object _runLock = new object();
        private static CancellationTokenSource _crawlCts;
        private static CancellationTokenSource _indexCts;

        public const string CrawlStatusFile = "crawl_status.json";
        public const string IndexStatusFile = "index_status.json";
        public const string SourcesStatusFile = "sources_status.json";
        public const string SchedulerConfigFile = "scheduler_config.json";

        // ---- Rutas (ancladas a App_Data) -------------------------------------------------

        public static string AppDataDir
        {
            get { return HostingEnvironment.MapPath("~/App_Data"); }
        }

        public static string StatusDir
        {
            get { return Path.Combine(AppDataDir ?? string.Empty, "status"); }
        }

        public static string LogsDir
        {
            get { return Path.Combine(AppDataDir ?? string.Empty, "logs"); }
        }

        public static string ProjectRoot
        {
            get { return (HostingEnvironment.MapPath("~") ?? string.Empty).TrimEnd('\\', '/'); }
        }

        /// <summary>
        /// Crea las carpetas de estado/logs si no existen. Idempotente.
        /// </summary>
        public static void EnsureFolders()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(StatusDir))
                    Directory.CreateDirectory(StatusDir);
                if (!string.IsNullOrWhiteSpace(LogsDir))
                    Directory.CreateDirectory(LogsDir);
            }
            catch
            {
                // No bloquear el arranque de la app por un fallo de E/S aqui.
            }
        }

        private static string StatusPath(string fileName)
        {
            return Path.Combine(StatusDir, fileName);
        }

        // ---- Lectura/escritura tipada de los ficheros de estado --------------------------

        public static JobRunStatus ReadCrawlStatus()
        {
            return JsonFile.ReadOrDefault(StatusPath(CrawlStatusFile), NewIdle("crawl"));
        }

        public static void WriteCrawlStatus(JobRunStatus status)
        {
            JsonFile.WriteAtomic(StatusPath(CrawlStatusFile), status);
        }

        public static JobRunStatus ReadIndexStatus()
        {
            return JsonFile.ReadOrDefault(StatusPath(IndexStatusFile), NewIdle("index"));
        }

        public static void WriteIndexStatus(JobRunStatus status)
        {
            JsonFile.WriteAtomic(StatusPath(IndexStatusFile), status);
        }

        public static SourcesStatusFile ReadSources()
        {
            return JsonFile.ReadOrDefault(StatusPath(SourcesStatusFile), new SourcesStatusFile());
        }

        public static void WriteSources(SourcesStatusFile sources)
        {
            if (sources != null)
                sources.UpdatedAt = DateTime.UtcNow.ToString("o");
            JsonFile.WriteAtomic(StatusPath(SourcesStatusFile), sources);
        }

        public static SchedulerConfig ReadScheduler()
        {
            return JsonFile.ReadOrDefault(StatusPath(SchedulerConfigFile), new SchedulerConfig());
        }

        public static void WriteScheduler(SchedulerConfig config)
        {
            JsonFile.WriteAtomic(StatusPath(SchedulerConfigFile), config);
        }

        private static JobRunStatus NewIdle(string kind)
        {
            return new JobRunStatus { Kind = kind, State = JobStates.Idle };
        }

        // ---- Single-flight + cancelacion -------------------------------------------------

        public static bool TryBeginCrawl(out CancellationToken token)
        {
            lock (_runLock)
            {
                if (_crawlCts != null)
                {
                    token = CancellationToken.None;
                    return false;
                }

                _crawlCts = new CancellationTokenSource();
                token = _crawlCts.Token;
                return true;
            }
        }

        public static void EndCrawl()
        {
            lock (_runLock)
            {
                if (_crawlCts != null)
                {
                    _crawlCts.Dispose();
                    _crawlCts = null;
                }
            }
        }

        public static bool IsCrawlRunning
        {
            get { lock (_runLock) { return _crawlCts != null; } }
        }

        public static bool RequestStopCrawl()
        {
            lock (_runLock)
            {
                if (_crawlCts == null)
                    return false;
                try { _crawlCts.Cancel(); } catch { }
                return true;
            }
        }

        public static bool TryBeginIndex(out CancellationToken token)
        {
            lock (_runLock)
            {
                if (_indexCts != null)
                {
                    token = CancellationToken.None;
                    return false;
                }

                _indexCts = new CancellationTokenSource();
                token = _indexCts.Token;
                return true;
            }
        }

        public static void EndIndex()
        {
            lock (_runLock)
            {
                if (_indexCts != null)
                {
                    _indexCts.Dispose();
                    _indexCts = null;
                }
            }
        }

        public static bool IsIndexRunning
        {
            get { lock (_runLock) { return _indexCts != null; } }
        }

        public static bool RequestStopIndex()
        {
            lock (_runLock)
            {
                if (_indexCts == null)
                    return false;
                try { _indexCts.Cancel(); } catch { }
                return true;
            }
        }

        /// <summary>
        /// Al arrancar la aplicacion, cualquier estado que quedo en "running" por un reciclaje
        /// del app pool ya no tiene un proceso vivo detras: lo marcamos como "stopped".
        /// </summary>
        public static void ReconcileOnStartup()
        {
            try
            {
                JobRunStatus crawl = ReadCrawlStatus();
                if (crawl != null && crawl.State == JobStates.Running)
                {
                    crawl.State = JobStates.Stopped;
                    crawl.LastError = "Interrumpido por reinicio de la aplicacion.";
                    crawl.FinishedAt = DateTime.UtcNow.ToString("o");
                    WriteCrawlStatus(crawl);
                }

                JobRunStatus index = ReadIndexStatus();
                if (index != null && index.State == JobStates.Running)
                {
                    index.State = JobStates.Stopped;
                    index.LastError = "Interrumpido por reinicio de la aplicacion.";
                    index.FinishedAt = DateTime.UtcNow.ToString("o");
                    WriteIndexStatus(index);
                }
            }
            catch
            {
            }
        }
    }
}
