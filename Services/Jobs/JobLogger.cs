using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace rag_can_aspx.Services.Jobs
{
    /// <summary>
    /// Logger sencillo de ficheros para los jobs (App_Data/logs/crawler.log e indexer.log).
    /// Cada linea lleva timestamp UTC. Rota el fichero a ".1" cuando supera ~5 MB.
    /// </summary>
    public static class JobLogger
    {
        public const string CrawlerLog = "crawler.log";
        public const string IndexerLog = "indexer.log";

        private const long MaxBytes = 5 * 1024 * 1024;
        private static readonly object _lock = new object();

        public static void Append(string logName, string message)
        {
            if (string.IsNullOrWhiteSpace(logName))
                return;

            try
            {
                string dir = JobStatusManager.LogsDir;
                if (string.IsNullOrWhiteSpace(dir))
                    return;

                lock (_lock)
                {
                    Directory.CreateDirectory(dir);
                    string path = Path.Combine(dir, logName);

                    RotateIfNeeded(path);

                    string line = string.Format("{0:o}  {1}{2}", DateTime.UtcNow, message ?? string.Empty, Environment.NewLine);
                    File.AppendAllText(path, line, new UTF8Encoding(false));
                }
            }
            catch
            {
                // Nunca dejar que un fallo de logging tumbe un job.
            }
        }

        /// <summary>
        /// Devuelve las ultimas <paramref name="lines"/> lineas del log indicado (mas recientes al final).
        /// </summary>
        public static List<string> Tail(string logName, int lines)
        {
            var result = new List<string>();
            if (lines <= 0)
                return result;

            try
            {
                string dir = JobStatusManager.LogsDir;
                if (string.IsNullOrWhiteSpace(dir))
                    return result;

                string path = Path.Combine(dir, logName);
                if (!File.Exists(path))
                    return result;

                lock (_lock)
                {
                    string[] all = File.ReadAllLines(path, Encoding.UTF8);
                    result = all.Reverse().Take(lines).Reverse().ToList();
                }
            }
            catch
            {
            }

            return result;
        }

        private static void RotateIfNeeded(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                var info = new FileInfo(path);
                if (info.Length < MaxBytes)
                    return;

                string backup = path + ".1";
                if (File.Exists(backup))
                    File.Delete(backup);
                File.Move(path, backup);
            }
            catch
            {
            }
        }
    }
}
