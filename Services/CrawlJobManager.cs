using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace rag_can_aspx.Services
{
    public static class CrawlJobManager
    {
        private static readonly ConcurrentDictionary<string, CrawlJobInfo> _jobs =
            new ConcurrentDictionary<string, CrawlJobInfo>(StringComparer.OrdinalIgnoreCase);

        public static CrawlJobInfo QueueJob(CrawlJobRequest request, string applicationRootPath)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            PurgarTrabajosAntiguos(request.Settings.JobStatusRetentionMinutes);

            var crawler = new CrawlerService(request.Settings, applicationRootPath);
            var job = new CrawlJobInfo
            {
                JobId = Guid.NewGuid().ToString("N"),
                Status = CrawlJobState.Queued,
                CreatedAtUtc = DateTime.UtcNow,
                BaseFolderAbsolute = request.BaseFolderAbsolute,
                BaseFolderRelative = PathHelper.ObtenerRutaRelativa(applicationRootPath, request.BaseFolderAbsolute),
                MaxPages = request.MaxPages,
                MaxDepth = request.MaxDepth,
                Domains = request.SeedUrls
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(seed => new DomainCrawlInfo
                    {
                        SeedUrl = seed,
                        Host = ObtenerHost(seed),
                        Status = CrawlJobState.Queued
                    })
                    .ToList()
            };

            _jobs[job.JobId] = job;

            HostingEnvironment.QueueBackgroundWorkItem(async token =>
            {
                await EjecutarJobAsync(job, request, crawler, applicationRootPath, token).ConfigureAwait(false);
            });

            return job;
        }

        public static CrawlJobInfo GetJob(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                return null;

            CrawlJobInfo job;
            return _jobs.TryGetValue(jobId, out job) ? job : null;
        }

        private static async Task EjecutarJobAsync(
            CrawlJobInfo job,
            CrawlJobRequest request,
            CrawlerService crawler,
            string applicationRootPath,
            CancellationToken cancellationToken)
        {
            job.Status = CrawlJobState.Running;
            job.StartedAtUtc = DateTime.UtcNow;

            var semaphore = new SemaphoreSlim(request.Settings.MaxConcurrentDomains);
            var tareas = new List<Task>();

            foreach (var domain in job.Domains)
            {
                tareas.Add(EjecutarDominioAsync(
                    job,
                    domain,
                    request,
                    crawler,
                    applicationRootPath,
                    semaphore,
                    cancellationToken));
            }

            try
            {
                await Task.WhenAll(tareas).ConfigureAwait(false);
                job.Status = job.Domains.All(d => d.Status == CrawlJobState.Failed)
                    ? CrawlJobState.Failed
                    : CrawlJobState.Completed;
            }
            catch (Exception ex)
            {
                job.Status = CrawlJobState.Failed;
                job.ErrorMessage = ex.GetBaseException().Message;
            }
            finally
            {
                job.FinishedAtUtc = DateTime.UtcNow;
                semaphore.Dispose();
            }
        }

        private static async Task EjecutarDominioAsync(
            CrawlJobInfo job,
            DomainCrawlInfo domain,
            CrawlJobRequest request,
            CrawlerService crawler,
            string applicationRootPath,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                domain.Status = CrawlJobState.Running;
                domain.StartedAtUtc = DateTime.UtcNow;

                Uri startUri;
                if (!Uri.TryCreate(domain.SeedUrl, UriKind.Absolute, out startUri))
                {
                    domain.Status = CrawlJobState.Failed;
                    domain.Message = $"URL inválida: {domain.SeedUrl}";
                    domain.FinishedAtUtc = DateTime.UtcNow;
                    return;
                }

                string nombreCarpeta = crawler.GenerarNombreCarpetaDominio(startUri);
                string carpetaSitio = Path.Combine(request.BaseFolderAbsolute, nombreCarpeta);
                string rutaRelativa = PathHelper.ObtenerRutaRelativa(applicationRootPath, carpetaSitio);

                var resultado = await crawler
                    .CrawlDominioAsync(domain.SeedUrl, carpetaSitio, request.MaxPages, request.MaxDepth, cancellationToken)
                    .ConfigureAwait(false);

                domain.PagesDownloaded = resultado.PaginasDescargadas;
                domain.OutputFolderRelative = rutaRelativa;
                domain.Message = resultado.Mensaje;
                domain.Status = resultado.Exitoso ? CrawlJobState.Completed : CrawlJobState.Failed;
                domain.FinishedAtUtc = DateTime.UtcNow;
            }
            catch (OperationCanceledException)
            {
                domain.Status = CrawlJobState.Failed;
                domain.Message = "Trabajo cancelado por el host.";
                domain.FinishedAtUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                domain.Status = CrawlJobState.Failed;
                domain.Message = ex.GetBaseException().Message;
                domain.FinishedAtUtc = DateTime.UtcNow;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private static void PurgarTrabajosAntiguos(int retentionMinutes)
        {
            DateTime limiteUtc = DateTime.UtcNow.AddMinutes(-retentionMinutes);
            foreach (var item in _jobs.ToArray())
            {
                var job = item.Value;
                if (job.FinishedAtUtc.HasValue && job.FinishedAtUtc.Value < limiteUtc)
                {
                    CrawlJobInfo eliminado;
                    _jobs.TryRemove(item.Key, out eliminado);
                }
            }
        }

        private static string ObtenerHost(string seed)
        {
            Uri uri;
            return Uri.TryCreate(seed, UriKind.Absolute, out uri) ? uri.Host : seed;
        }
    }

    public sealed class CrawlJobRequest
    {
        public List<string> SeedUrls { get; set; }
        public string BaseFolderAbsolute { get; set; }
        public int MaxPages { get; set; }
        public int MaxDepth { get; set; }
        public CrawlerSettings Settings { get; set; }
    }

    public sealed class CrawlJobInfo
    {
        public string JobId { get; set; }
        public CrawlJobState Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? FinishedAtUtc { get; set; }
        public string BaseFolderAbsolute { get; set; }
        public string BaseFolderRelative { get; set; }
        public int MaxPages { get; set; }
        public int MaxDepth { get; set; }
        public string ErrorMessage { get; set; }
        public List<DomainCrawlInfo> Domains { get; set; }
    }

    public sealed class DomainCrawlInfo
    {
        public string SeedUrl { get; set; }
        public string Host { get; set; }
        public CrawlJobState Status { get; set; }
        public int PagesDownloaded { get; set; }
        public string Message { get; set; }
        public string OutputFolderRelative { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? FinishedAtUtc { get; set; }
    }

    public enum CrawlJobState
    {
        Queued = 0,
        Running = 1,
        Completed = 2,
        Failed = 3
    }
}
