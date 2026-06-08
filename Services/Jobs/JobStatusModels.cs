using System.Collections.Generic;
using Newtonsoft.Json;

namespace rag_can_aspx.Services.Jobs
{
    /// <summary>
    /// Estados posibles de un job. Se serializan como texto para que los JSON sean legibles.
    /// </summary>
    public static class JobStates
    {
        public const string Idle = "idle";
        public const string Running = "running";
        public const string Completed = "completed";
        public const string Error = "error";
        public const string Stopped = "stopped";
    }

    /// <summary>
    /// Estado de una ejecucion (crawl o index). Es lo que se persiste en
    /// App_Data/status/crawl_status.json e index_status.json.
    /// </summary>
    public class JobRunStatus
    {
        [JsonProperty("kind")]
        public string Kind { get; set; } // "crawl" | "index"

        [JsonProperty("run_id")]
        public string RunId { get; set; }

        [JsonProperty("state")]
        public string State { get; set; } = JobStates.Idle;

        [JsonProperty("started_at")]
        public string StartedAt { get; set; }

        [JsonProperty("finished_at")]
        public string FinishedAt { get; set; }

        [JsonProperty("total_sources")]
        public int TotalSources { get; set; }

        [JsonProperty("processed_sources")]
        public int ProcessedSources { get; set; }

        [JsonProperty("failed_sources")]
        public int FailedSources { get; set; }

        [JsonProperty("skipped_sources")]
        public int SkippedSources { get; set; }

        [JsonProperty("current_url")]
        public string CurrentUrl { get; set; }

        [JsonProperty("last_error")]
        public string LastError { get; set; }

        [JsonProperty("progress_percent")]
        public int ProgressPercent { get; set; }

        public bool IsActive
        {
            get { return State == JobStates.Running; }
        }
    }

    /// <summary>
    /// Estado por fuente (semilla/URL) que se guarda en App_Data/status/sources_status.json.
    /// </summary>
    public class SourceStatus
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("host")]
        public string Host { get; set; }

        [JsonProperty("last_crawled_at")]
        public string LastCrawledAt { get; set; }

        [JsonProperty("http_status")]
        public int HttpStatus { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("content_sha256")]
        public string ContentSha256 { get; set; }

        [JsonProperty("txt_path")]
        public string TxtPath { get; set; }

        [JsonProperty("html_path")]
        public string HtmlPath { get; set; }

        [JsonProperty("metadata_path")]
        public string MetadataPath { get; set; }

        [JsonProperty("needs_index")]
        public bool NeedsIndex { get; set; }

        [JsonProperty("last_indexed_at")]
        public string LastIndexedAt { get; set; }

        [JsonProperty("chunk_count")]
        public int ChunkCount { get; set; }

        [JsonProperty("pages_total")]
        public int PagesTotal { get; set; }

        [JsonProperty("pages_changed")]
        public int PagesChanged { get; set; }

        [JsonProperty("pages_skipped")]
        public int PagesSkipped { get; set; }

        [JsonProperty("state")]
        public string State { get; set; } // ok | skipped | failed

        [JsonProperty("last_error")]
        public string LastError { get; set; }
    }

    /// <summary>
    /// Contenedor del fichero sources_status.json.
    /// </summary>
    public class SourcesStatusFile
    {
        [JsonProperty("updated_at")]
        public string UpdatedAt { get; set; }

        [JsonProperty("sources")]
        public List<SourceStatus> Sources { get; set; } = new List<SourceStatus>();
    }

    /// <summary>
    /// Configuracion del scheduler (App_Data/status/scheduler_config.json).
    /// </summary>
    public class SchedulerConfig
    {
        [JsonProperty("crawl_enabled")]
        public bool CrawlEnabled { get; set; }

        [JsonProperty("index_enabled")]
        public bool IndexEnabled { get; set; }

        // "manual" | "interval" | "daily"
        [JsonProperty("mode")]
        public string Mode { get; set; } = "manual";

        [JsonProperty("interval_hours")]
        public int IntervalHours { get; set; } = 24;

        // Formato "HH:mm" (hora local del servidor).
        [JsonProperty("daily_time")]
        public string DailyTime { get; set; } = "03:00";

        [JsonProperty("last_run_at")]
        public string LastRunAt { get; set; }

        [JsonProperty("next_run_at")]
        public string NextRunAt { get; set; }
    }
}
