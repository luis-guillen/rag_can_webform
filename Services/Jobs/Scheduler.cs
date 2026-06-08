using System;
using System.Threading;

namespace rag_can_aspx.Services.Jobs
{
    /// <summary>
    /// Scheduler in-process (NIVEL 1). Un Timer revisa cada minuto la configuracion
    /// (App_Data/status/scheduler_config.json) y, si toca y no hay jobs corriendo, lanza un
    /// ciclo crawl -> index. Requiere que el app pool este vivo (ver README: keep-alive /
    /// Application Initialization). Para produccion/500 webs se documenta el uso de la Tarea
    /// programada de Windows que invoque el endpoint/metodo de la fachada.
    /// </summary>
    public static class Scheduler
    {
        private static readonly object _lock = new object();
        private static Timer _timer;

        public static void Start()
        {
            lock (_lock)
            {
                if (_timer != null)
                    return;

                // Primer tick al minuto, luego cada minuto.
                _timer = new Timer(Tick, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
            }
        }

        public static void Stop()
        {
            lock (_lock)
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }

        private static void Tick(object state)
        {
            try
            {
                SchedulerConfig cfg = JobStatusManager.ReadScheduler();
                if (cfg == null || cfg.Mode == "manual")
                    return;

                if (!cfg.CrawlEnabled && !cfg.IndexEnabled)
                    return;

                if (JobStatusManager.IsCrawlRunning || JobStatusManager.IsIndexRunning)
                    return;

                DateTime nowUtc = DateTime.UtcNow;
                if (!EsDue(cfg, nowUtc))
                    return;

                var facade = new CrawlerIndexerFacade();
                JobActionResult result = facade.StartScheduledCycle(cfg.CrawlEnabled, cfg.IndexEnabled);
                JobLogger.Append(JobLogger.CrawlerLog, "[scheduler] " + result.Message);

                cfg.LastRunAt = nowUtc.ToString("o");
                cfg.NextRunAt = ComputeNext(cfg, nowUtc).ToString("o");
                JobStatusManager.WriteScheduler(cfg);
            }
            catch
            {
                // Nunca dejar que el tick tumbe el timer.
            }
        }

        private static bool EsDue(SchedulerConfig cfg, DateTime nowUtc)
        {
            DateTime? last = ParseUtc(cfg.LastRunAt);

            if (string.Equals(cfg.Mode, "interval", StringComparison.OrdinalIgnoreCase))
            {
                if (last == null)
                    return true;
                return (nowUtc - last.Value) >= TimeSpan.FromHours(Math.Max(1, cfg.IntervalHours));
            }

            if (string.Equals(cfg.Mode, "daily", StringComparison.OrdinalIgnoreCase))
            {
                DateTime nowLocal = nowUtc.ToLocalTime();
                TimeSpan tod;
                if (!TimeSpan.TryParse(string.IsNullOrWhiteSpace(cfg.DailyTime) ? "03:00" : cfg.DailyTime, out tod))
                    tod = new TimeSpan(3, 0, 0);

                DateTime scheduledLocalToday = nowLocal.Date + tod;
                if (nowLocal < scheduledLocalToday)
                    return false;

                if (last == null)
                    return true;

                return last.Value.ToLocalTime() < scheduledLocalToday;
            }

            return false;
        }

        private static DateTime ComputeNext(SchedulerConfig cfg, DateTime nowUtc)
        {
            if (string.Equals(cfg.Mode, "interval", StringComparison.OrdinalIgnoreCase))
                return nowUtc.AddHours(Math.Max(1, cfg.IntervalHours));

            if (string.Equals(cfg.Mode, "daily", StringComparison.OrdinalIgnoreCase))
            {
                DateTime nowLocal = nowUtc.ToLocalTime();
                TimeSpan tod;
                if (!TimeSpan.TryParse(string.IsNullOrWhiteSpace(cfg.DailyTime) ? "03:00" : cfg.DailyTime, out tod))
                    tod = new TimeSpan(3, 0, 0);

                DateTime next = nowLocal.Date + tod;
                if (next <= nowLocal)
                    next = next.AddDays(1);
                return next.ToUniversalTime();
            }

            return nowUtc;
        }

        private static DateTime? ParseUtc(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            DateTime parsed;
            if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out parsed))
                return parsed;
            return null;
        }
    }
}
