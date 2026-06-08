using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Activation;
using rag_can_aspx.Services.Jobs;

namespace rag_can_aspx.Services.Wcf
{
    /// <summary>
    /// Implementacion WCF delgada: solo delega en CrawlerIndexerFacade y mapea a DTOs.
    /// AspNetCompatibility = Allowed para correr en el pipeline de ASP.NET WebForms
    /// (necesario para que HostingEnvironment/App_Data funcionen igual que en las paginas).
    /// </summary>
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class CrawlerIndexerService : ICrawlerIndexerService
    {
        private readonly CrawlerIndexerFacade _facade = new CrawlerIndexerFacade();

        public ActionResultDto StartCrawl()
        {
            return Map(_facade.StartCrawl());
        }

        public ActionResultDto StartCrawlSource(string url)
        {
            return Map(_facade.StartCrawlSource(url));
        }

        public ActionResultDto StopCrawl()
        {
            return Map(_facade.StopCrawl());
        }

        public JobStatusDto GetCrawlStatus()
        {
            return Map(_facade.GetCrawlStatus());
        }

        public JobStatusDto GetLastCrawlRun()
        {
            return Map(_facade.GetLastCrawlRun());
        }

        public ActionResultDto StartIndexing()
        {
            return Map(_facade.StartIndexing());
        }

        public ActionResultDto StopIndexing()
        {
            return Map(_facade.StopIndexing());
        }

        public JobStatusDto GetIndexingStatus()
        {
            return Map(_facade.GetIndexingStatus());
        }

        public JobStatusDto GetLastIndexingRun()
        {
            return Map(_facade.GetLastIndexingRun());
        }

        public List<SourceStatusDto> GetSources()
        {
            return _facade.GetSources().Select(Map).ToList();
        }

        public SourceStatusDto GetSourceStatus(string url)
        {
            return Map(_facade.GetSourceStatus(url));
        }

        public LogsDto GetLogs(int lines)
        {
            LogsSnapshot snapshot = _facade.GetLogs(lines);
            return new LogsDto
            {
                Crawler = snapshot.Crawler,
                Indexer = snapshot.Indexer
            };
        }

        // -------- mapeo interno -> DTO --------

        private static ActionResultDto Map(JobActionResult result)
        {
            if (result == null)
                return new ActionResultDto { Accepted = false, Message = "Sin respuesta." };

            return new ActionResultDto { Accepted = result.Accepted, Message = result.Message };
        }

        private static JobStatusDto Map(JobRunStatus status)
        {
            if (status == null)
                return null;

            return new JobStatusDto
            {
                Kind = status.Kind,
                RunId = status.RunId,
                State = status.State,
                StartedAt = status.StartedAt,
                FinishedAt = status.FinishedAt,
                TotalSources = status.TotalSources,
                ProcessedSources = status.ProcessedSources,
                FailedSources = status.FailedSources,
                SkippedSources = status.SkippedSources,
                CurrentUrl = status.CurrentUrl,
                LastError = status.LastError,
                ProgressPercent = status.ProgressPercent
            };
        }

        private static SourceStatusDto Map(SourceStatus source)
        {
            if (source == null)
                return null;

            return new SourceStatusDto
            {
                Url = source.Url,
                Host = source.Host,
                LastCrawledAt = source.LastCrawledAt,
                HttpStatus = source.HttpStatus,
                Title = source.Title,
                NeedsIndex = source.NeedsIndex,
                LastIndexedAt = source.LastIndexedAt,
                ChunkCount = source.ChunkCount,
                PagesTotal = source.PagesTotal,
                PagesChanged = source.PagesChanged,
                PagesSkipped = source.PagesSkipped,
                State = source.State,
                LastError = source.LastError
            };
        }
    }
}
