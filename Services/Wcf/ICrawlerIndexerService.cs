using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace rag_can_aspx.Services.Wcf
{
    /// <summary>
    /// Contrato WCF que expone la capa de control (CrawlerIndexerFacade) como servicio.
    /// Es un wrapper delgado: cada operacion delega en la fachada y mapea a DTOs propios
    /// para no exponer los tipos internos.
    /// </summary>
    [ServiceContract(Namespace = "http://ragcan/crawler-indexer")]
    public interface ICrawlerIndexerService
    {
        [OperationContract]
        ActionResultDto StartCrawl();

        [OperationContract]
        ActionResultDto StartCrawlSource(string url);

        [OperationContract]
        ActionResultDto StopCrawl();

        [OperationContract]
        JobStatusDto GetCrawlStatus();

        [OperationContract]
        JobStatusDto GetLastCrawlRun();

        [OperationContract]
        ActionResultDto StartIndexing();

        [OperationContract]
        ActionResultDto StopIndexing();

        [OperationContract]
        JobStatusDto GetIndexingStatus();

        [OperationContract]
        JobStatusDto GetLastIndexingRun();

        [OperationContract]
        List<SourceStatusDto> GetSources();

        [OperationContract]
        SourceStatusDto GetSourceStatus(string url);

        [OperationContract]
        LogsDto GetLogs(int lines);
    }

    [DataContract]
    public class ActionResultDto
    {
        [DataMember] public bool Accepted { get; set; }
        [DataMember] public string Message { get; set; }
    }

    [DataContract]
    public class JobStatusDto
    {
        [DataMember] public string Kind { get; set; }
        [DataMember] public string RunId { get; set; }
        [DataMember] public string State { get; set; }
        [DataMember] public string StartedAt { get; set; }
        [DataMember] public string FinishedAt { get; set; }
        [DataMember] public int TotalSources { get; set; }
        [DataMember] public int ProcessedSources { get; set; }
        [DataMember] public int FailedSources { get; set; }
        [DataMember] public int SkippedSources { get; set; }
        [DataMember] public string CurrentUrl { get; set; }
        [DataMember] public string LastError { get; set; }
        [DataMember] public int ProgressPercent { get; set; }
    }

    [DataContract]
    public class SourceStatusDto
    {
        [DataMember] public string Url { get; set; }
        [DataMember] public string Host { get; set; }
        [DataMember] public string LastCrawledAt { get; set; }
        [DataMember] public int HttpStatus { get; set; }
        [DataMember] public string Title { get; set; }
        [DataMember] public bool NeedsIndex { get; set; }
        [DataMember] public string LastIndexedAt { get; set; }
        [DataMember] public int ChunkCount { get; set; }
        [DataMember] public int PagesTotal { get; set; }
        [DataMember] public int PagesChanged { get; set; }
        [DataMember] public int PagesSkipped { get; set; }
        [DataMember] public string State { get; set; }
        [DataMember] public string LastError { get; set; }
    }

    [DataContract]
    public class LogsDto
    {
        [DataMember] public List<string> Crawler { get; set; }
        [DataMember] public List<string> Indexer { get; set; }
    }
}
