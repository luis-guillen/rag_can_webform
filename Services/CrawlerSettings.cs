using System;
using System.Configuration;

namespace rag_can_aspx.Services
{
    public sealed class CrawlerSettings
    {
        public string SeedsFile { get; private set; }
        public int MaxConcurrentDomains { get; private set; }
        public int RequestDelayMs { get; private set; }
        public int HttpTimeoutSeconds { get; private set; }
        public int JobStatusRetentionMinutes { get; private set; }

        public static CrawlerSettings Load()
        {
            return new CrawlerSettings
            {
                SeedsFile = ReadString("Crawler:SeedsFile", "~/App_Data/seeds.txt"),
                MaxConcurrentDomains = ReadInt("Crawler:MaxConcurrentDomains", 3, 1, 16),
                RequestDelayMs = ReadInt("Crawler:RequestDelayMs", 300, 0, 10000),
                HttpTimeoutSeconds = ReadInt("Crawler:HttpTimeoutSeconds", 15, 5, 300),
                JobStatusRetentionMinutes = ReadInt("Crawler:JobStatusRetentionMinutes", 60, 5, 1440)
            };
        }

        private static string ReadString(string key, string defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        }

        private static int ReadInt(string key, int defaultValue, int minValue, int maxValue)
        {
            string rawValue = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(rawValue))
                return defaultValue;

            int parsedValue;
            if (!int.TryParse(rawValue, out parsedValue))
                throw new ConfigurationErrorsException($"La clave '{key}' debe ser numérica.");

            if (parsedValue < minValue || parsedValue > maxValue)
                throw new ConfigurationErrorsException(
                    $"La clave '{key}' debe estar entre {minValue} y {maxValue}.");

            return parsedValue;
        }
    }
}
