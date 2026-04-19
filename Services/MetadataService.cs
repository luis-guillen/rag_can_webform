using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace rag_can_aspx.Services
{
    public class PageMetadata
    {
        [JsonProperty("job")]
        public string Job { get; set; }

        [JsonProperty("domain")]
        public string Domain { get; set; }

        [JsonProperty("page_number")]
        public int PageNumber { get; set; }

        [JsonProperty("file")]
        public string File { get; set; }

        [JsonProperty("crawled_at")]
        public string CrawledAt { get; set; }

        [JsonProperty("chars")]
        public int Chars { get; set; }

        [JsonProperty("sha256")]
        public string Sha256 { get; set; }

        [JsonProperty("quality")]
        public string Quality { get; set; }

        [JsonProperty("duplicate_of")]
        public string DuplicateOf { get; set; }

        [JsonProperty("manual")]
        public ManualMetadata Manual { get; set; }
    }

    public class ManualMetadata
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("topics")]
        public List<string> Topics { get; set; }

        [JsonProperty("license")]
        public string License { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }
    }

    public class MetadataService
    {
        private readonly string _metadataPath;
        private readonly string _projectRoot;

        private static readonly Dictionary<string, string[]> _topicMap =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "cultura",    new[] { "cultura" } },
                { "museo",      new[] { "museo", "patrimonio" } },
                { "memoria",    new[] { "memoria histórica" } },
                { "academia",   new[] { "lengua", "academia canaria" } },
                { "lengua",     new[] { "lengua", "lingüística" } },
                { "lanzarote",  new[] { "lanzarote", "canarias" } },
                { "grancanaria",new[] { "gran canaria", "canarias" } },
                { "ulpgc",      new[] { "universidad", "investigación" } },
                { "iatext",     new[] { "atlas", "lingüística", "canarias" } },
                { "blogspot",   new[] { "blog" } },
                { "canaria",    new[] { "canarias" } },
                { "izuran",     new[] { "amazigh", "cultura" } },
            };

        public MetadataService(string projectRoot)
        {
            _projectRoot = projectRoot.TrimEnd('\\', '/');
            _metadataPath = Path.Combine(_projectRoot, "metadata.json");
        }

        public PageMetadata BuildForNewPage(
            string absoluteFilePath,
            string url,
            string htmlTitle,
            string jobName,
            int pageNumber,
            DateTime crawledAtUtc)
        {
            string text = ReadClean(absoluteFilePath);
            string relFile = ToRelative(absoluteFilePath);
            string domain = ExtractDomainFromPath(relFile);

            string titleValue = string.IsNullOrWhiteSpace(htmlTitle)
                ? $"(auto) {domain} — página {pageNumber}"
                : htmlTitle;

            return Build(text, relFile, url, titleValue, jobName, domain, pageNumber, crawledAtUtc);
        }

        public PageMetadata BuildForExistingPage(string absoluteFilePath, string jobName)
        {
            string text = ReadClean(absoluteFilePath);
            string relFile = ToRelative(absoluteFilePath);
            string domain = ExtractDomainFromPath(relFile);
            int pageNumber = ExtractPageNumber(Path.GetFileName(absoluteFilePath));
            string url = ReconstructUrl(domain, Path.GetFileName(absoluteFilePath));
            string title = ExtractFirstLine(text);
            DateTime crawledAt = System.IO.File.GetLastWriteTimeUtc(absoluteFilePath);

            return Build(text, relFile, url, title, jobName, domain, pageNumber, crawledAt);
        }

        private PageMetadata Build(
            string text, string relFile, string url, string title,
            string jobName, string domain, int pageNumber, DateTime crawledAt)
        {
            string sha = DuplicateDetector.Sha256Hex(text);
            Quality q = QualityScorer.Score(text);

            return new PageMetadata
            {
                Job = jobName,
                Domain = domain,
                PageNumber = pageNumber,
                File = relFile,
                CrawledAt = crawledAt.ToString("o"),
                Chars = text?.Length ?? 0,
                Sha256 = sha,
                Quality = QualityScorer.ToLabel(q),
                DuplicateOf = null,
                Manual = new ManualMetadata
                {
                    Url = string.IsNullOrWhiteSpace(url) ? $"(auto) https://{DomainToHost(domain)}/" : url,
                    Title = title,
                    Description = $"(auto) {domain} — página {pageNumber}",
                    Topics = InferTopics(domain),
                    License = "(auto) desconocida",
                    Language = "es"
                }
            };
        }

        public void UpsertAndSave(PageMetadata entry)
        {
            var all = LoadAll();
            int idx = all.FindIndex(e => string.Equals(e.File, entry.File, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
                all[idx] = entry;
            else
                all.Add(entry);

            SaveAll(all);
        }

        public void ResolveDuplicates(List<PageMetadata> entries)
        {
            var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.Sha256))
                    continue;

                if (seen.ContainsKey(e.Sha256))
                    e.DuplicateOf = seen[e.Sha256];
                else
                    seen[e.Sha256] = e.File;
            }
        }

        public List<PageMetadata> LoadAll()
        {
            if (!System.IO.File.Exists(_metadataPath))
                return new List<PageMetadata>();

            try
            {
                string json = System.IO.File.ReadAllText(_metadataPath, Encoding.UTF8);
                return JsonConvert.DeserializeObject<List<PageMetadata>>(json)
                    ?? new List<PageMetadata>();
            }
            catch
            {
                return new List<PageMetadata>();
            }
        }

        public void SaveAll(List<PageMetadata> entries)
        {
            string json = JsonConvert.SerializeObject(entries, Formatting.Indented);
            string tmp = _metadataPath + ".tmp";
            System.IO.File.WriteAllText(tmp, json, new UTF8Encoding(false));

            if (System.IO.File.Exists(_metadataPath))
                System.IO.File.Replace(tmp, _metadataPath, null);
            else
                System.IO.File.Move(tmp, _metadataPath);
        }

        // ── helpers ──────────────────────────────────────────────────

        private string ReadClean(string absolutePath)
        {
            if (!System.IO.File.Exists(absolutePath))
                return string.Empty;

            string text = System.IO.File.ReadAllText(absolutePath, Encoding.UTF8);
            return text.TrimStart('\uFEFF');
        }

        private string ToRelative(string absolutePath)
        {
            string root = _projectRoot + Path.DirectorySeparatorChar;
            if (absolutePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return absolutePath.Substring(root.Length).Replace('\\', '/');
            return absolutePath.Replace('\\', '/');
        }

        private static string ExtractDomainFromPath(string relFile)
        {
            // relFile like "App_Data/p13/izuran_blogspot_com/01_index.txt"
            string[] parts = relFile.Replace('\\', '/').Split('/');
            // domain is the 3rd segment (index 2)
            return parts.Length >= 3 ? parts[parts.Length - 2] : "unknown";
        }

        private static int ExtractPageNumber(string filename)
        {
            int underscore = filename.IndexOf('_');
            if (underscore > 0)
            {
                int n;
                if (int.TryParse(filename.Substring(0, underscore), out n))
                    return n;
            }
            return 0;
        }

        private static string ReconstructUrl(string domain, string filename)
        {
            // domain: "www_academiacanarialengua_org" → "www.academiacanarialengua.org"
            string host = DomainToHost(domain);

            // filename: "03_diccionario_a.txt" → strip number prefix and .txt
            string path = filename;
            int underscore = path.IndexOf('_');
            if (underscore >= 0)
                path = path.Substring(underscore + 1);
            if (path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                path = path.Substring(0, path.Length - 4);

            path = path.Replace('_', '/').Trim('/');
            string url = path == "home" || string.IsNullOrWhiteSpace(path)
                ? $"(auto) https://{host}/"
                : $"(auto) https://{host}/{path}";

            return url;
        }

        private static string DomainToHost(string domain)
        {
            return domain.Replace('_', '.');
        }

        private static string ExtractFirstLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "(auto) sin título";

            foreach (string line in text.Split('\n'))
            {
                string trimmed = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    return trimmed.Length > 120 ? trimmed.Substring(0, 120) : trimmed;
            }
            return "(auto) sin título";
        }

        private static List<string> InferTopics(string domain)
        {
            var result = new List<string>();
            string lower = domain.ToLowerInvariant();

            foreach (var kv in _topicMap)
            {
                if (lower.Contains(kv.Key.ToLowerInvariant()))
                {
                    foreach (string t in kv.Value)
                        if (!result.Contains(t))
                            result.Add(t);
                }
            }

            if (result.Count == 0)
                result.Add("(auto) general");
            else
                result = result.Select(t => t.StartsWith("(auto)") ? t : "(auto) " + t).ToList();

            return result;
        }
    }
}
