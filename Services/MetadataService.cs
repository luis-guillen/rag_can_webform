using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace rag_can_aspx.Services
{
    public class ResourceMetadata
    {
        [JsonProperty("domain")]
        public string Domain { get; set; }

        [JsonProperty("domain_slug")]
        public string DomainSlug { get; set; }

        [JsonProperty("source_name")]
        public string SourceName { get; set; }

        [JsonProperty("source_type")]
        public string SourceType { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("island")]
        public string Island { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("license")]
        public string License { get; set; }

        [JsonProperty("topics")]
        public List<string> Topics { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("reliability")]
        public string Reliability { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; }
    }

    public class PageMetadata
    {
        [JsonProperty("job")]
        public string Job { get; set; }

        [JsonProperty("domain")]
        public string Domain { get; set; }

        [JsonProperty("domain_slug")]
        public string DomainSlug { get; set; }

        [JsonProperty("page_number")]
        public int PageNumber { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

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

        [JsonProperty("depth")]
        public int Depth { get; set; }
    }

    public class PageMetadataDocument
    {
        [JsonProperty("domain_metadata")]
        public ResourceMetadata DomainMetadata { get; set; }

        [JsonProperty("page_metadata")]
        public PageMetadata PageMetadata { get; set; }
    }

    public class RootMetadataEntry
    {
        [JsonProperty("domain")]
        public string Domain { get; set; }

        [JsonProperty("domain_slug")]
        public string DomainSlug { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("file")]
        public string File { get; set; }

        [JsonProperty("source_name")]
        public string SourceName { get; set; }

        [JsonProperty("source_type")]
        public string SourceType { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("island")]
        public string Island { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("license")]
        public string License { get; set; }

        [JsonProperty("topics")]
        public List<string> Topics { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class MetadataService
    {
        private readonly string _metadataPath;
        private readonly string _projectRoot;

        private class DomainMetadataSeed
        {
            public string SourceName { get; set; }
            public string SourceType { get; set; }
            public string Category { get; set; }
            public string Region { get; set; }
            public string Island { get; set; }
            public string License { get; set; }
            public string Reliability { get; set; }
            public string Notes { get; set; }
            public string Description { get; set; }
            public string[] Topics { get; set; }
        }

        private static readonly Dictionary<string, DomainMetadataSeed> _domainMap =
            new Dictionary<string, DomainMetadataSeed>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "canarias-azul_iatext_ulpgc_es",
                    new DomainMetadataSeed
                    {
                        SourceName = "Canarias Azul",
                        SourceType = "portal institucional universitario",
                        Category = "patrimonio litoral",
                        Region = "Canarias",
                        Island = "Canarias",
                        License = "desconocida",
                        Reliability = "alta",
                        Notes = "Metadatos manuales estimados a partir del dominio.",
                        Description = "Recurso universitario sobre patrimonio litoral y cultural de Canarias.",
                        Topics = new[] { "patrimonio litoral", "atlas", "universidad", "canarias" }
                    }
                },
                {
                    "cultura_grancanaria_com",
                    new DomainMetadataSeed
                    {
                        SourceName = "Gran Canaria Cultura",
                        SourceType = "portal institucional",
                        Category = "cultura",
                        Region = "Canarias",
                        Island = "Gran Canaria",
                        License = "desconocida",
                        Reliability = "alta",
                        Notes = "Metadatos manuales estimados a partir del dominio.",
                        Description = "Portal cultural del Cabildo de Gran Canaria.",
                        Topics = new[] { "cultura", "agenda cultural", "patrimonio", "gran canaria" }
                    }
                },
                {
                    "elmuseocanario_com",
                    new DomainMetadataSeed
                    {
                        SourceName = "El Museo Canario",
                        SourceType = "museo",
                        Category = "patrimonio",
                        Region = "Canarias",
                        Island = "Gran Canaria",
                        License = "desconocida",
                        Reliability = "alta",
                        Notes = "Metadatos manuales estimados a partir del dominio.",
                        Description = "Recurso cultural y museistico sobre patrimonio historico canario.",
                        Topics = new[] { "museo", "patrimonio", "arqueologia", "canarias" }
                    }
                },
                {
                    "izuran_blogspot_com",
                    new DomainMetadataSeed
                    {
                        SourceName = "Izuran",
                        SourceType = "blog",
                        Category = "cultura",
                        Region = "Canarias",
                        Island = "Canarias",
                        License = "desconocida",
                        Reliability = "media",
                        Notes = "Metadatos manuales estimados a partir del dominio.",
                        Description = "Blog sobre cultura amazigh y conexiones historicas con Canarias.",
                        Topics = new[] { "amazigh", "cultura", "lengua", "canarias" }
                    }
                },
                {
                    "memoriadelanzarote_com",
                    new DomainMetadataSeed
                    {
                        SourceName = "Memoria de Lanzarote",
                        SourceType = "archivo digital",
                        Category = "memoria histórica",
                        Region = "Canarias",
                        Island = "Lanzarote",
                        License = "desconocida",
                        Reliability = "alta",
                        Notes = "Metadatos manuales estimados a partir del dominio.",
                        Description = "Archivo digital de memoria histórica y documentación sobre Lanzarote.",
                        Topics = new[] { "memoria histórica", "archivo", "Lanzarote", "Canarias" }
                    }
                },
                {
                    "www_academiacanarialengua_org",
                    new DomainMetadataSeed
                    {
                        SourceName = "Academia Canaria de la Lengua",
                        SourceType = "institucion academica",
                        Category = "lengua",
                        Region = "Canarias",
                        Island = "Canarias",
                        License = "desconocida",
                        Reliability = "alta",
                        Notes = "Metadatos manuales estimados a partir del dominio.",
                        Description = "Recurso institucional sobre lengua y variedades linguisticas de Canarias.",
                        Topics = new[] { "lengua", "linguistica", "diccionario", "canarias" }
                    }
                }
            };

        private static readonly Dictionary<string, string[]> _topicMap =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "cultura",     new[] { "cultura" } },
                { "museo",       new[] { "museo", "patrimonio" } },
                { "memoria",     new[] { "memoria histórica" } },
                { "academia",    new[] { "lengua", "academia canaria" } },
                { "lengua",      new[] { "lengua", "linguistica" } },
                { "lanzarote",   new[] { "lanzarote", "canarias" } },
                { "grancanaria", new[] { "gran canaria", "canarias" } },
                { "ulpgc",       new[] { "universidad", "investigacion" } },
                { "iatext",      new[] { "atlas", "linguistica", "canarias" } },
                { "blogspot",    new[] { "blog" } },
                { "canaria",     new[] { "canarias" } },
                { "izuran",      new[] { "amazigh", "cultura" } },
            };

        public MetadataService(string projectRoot)
        {
            _projectRoot = projectRoot.TrimEnd('\\', '/');
            _metadataPath = Path.Combine(_projectRoot, "metadata.json");
        }

        public PageMetadataDocument BuildForNewPage(
            string absoluteFilePath,
            string url,
            string htmlTitle,
            string jobName,
            int pageNumber,
            DateTime crawledAtUtc,
            int depth)
        {
            string text = ReadClean(absoluteFilePath);
            string relFile = ToRelative(absoluteFilePath);
            string domain = ExtractDomainFromPath(relFile);

            string titleValue = string.IsNullOrWhiteSpace(htmlTitle)
                ? string.Format("(auto) {0} - pagina {1}", DomainToHost(domain), pageNumber)
                : htmlTitle;

            return Build(text, relFile, url, titleValue, jobName, domain, pageNumber, crawledAtUtc, depth);
        }

        public PageMetadataDocument BuildForExistingPage(string absoluteFilePath, string jobName)
        {
            string text = ReadClean(absoluteFilePath);
            string relFile = ToRelative(absoluteFilePath);
            string domain = ExtractDomainFromPath(relFile);
            int pageNumber = ExtractPageNumber(Path.GetFileName(absoluteFilePath));
            string url = ReconstructUrl(domain, Path.GetFileName(absoluteFilePath));
            string title = ExtractFirstLine(text);
            DateTime crawledAt = System.IO.File.GetLastWriteTimeUtc(absoluteFilePath);
            int depth = LoadExistingDepth(absoluteFilePath, url);

            return Build(text, relFile, url, title, jobName, domain, pageNumber, crawledAt, depth);
        }

        private PageMetadataDocument Build(
            string text,
            string relFile,
            string url,
            string title,
            string jobName,
            string domain,
            int pageNumber,
            DateTime crawledAt,
            int depth)
        {
            string sha = DuplicateDetector.Sha256Hex(text);
            Quality q = QualityScorer.Score(text);

            var pageMetadata = new PageMetadata
            {
                Job = jobName,
                Domain = DomainToHost(domain),
                DomainSlug = domain,
                PageNumber = pageNumber,
                Url = string.IsNullOrWhiteSpace(url) ? string.Format("https://{0}/", DomainToHost(domain)) : url,
                Title = title,
                File = relFile,
                CrawledAt = crawledAt.ToString("o"),
                Chars = text == null ? 0 : text.Length,
                Sha256 = sha,
                Quality = QualityScorer.ToLabel(q),
                DuplicateOf = null,
                Depth = depth
            };

            return new PageMetadataDocument
            {
                DomainMetadata = BuildResourceMetadata(domain),
                PageMetadata = pageMetadata
            };
        }

        public void UpsertAndSave(PageMetadataDocument entry)
        {
            SavePageMetadata(entry);

            var all = LoadAll();
            int idx = all.FindIndex(e => SameFile(e, entry));
            if (idx >= 0)
                all[idx] = entry;
            else
                all.Add(entry);

            ResolveDuplicates(all);
            SaveRootIndex(all);
        }

        public void ResolveDuplicates(List<PageMetadataDocument> entries)
        {
            var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in entries)
            {
                if (e == null || e.PageMetadata == null || string.IsNullOrEmpty(e.PageMetadata.Sha256))
                    continue;

                if (seen.ContainsKey(e.PageMetadata.Sha256))
                    e.PageMetadata.DuplicateOf = seen[e.PageMetadata.Sha256];
                else
                    seen[e.PageMetadata.Sha256] = e.PageMetadata.File;
            }
        }

        public List<PageMetadataDocument> LoadAll()
        {
            var result = new List<PageMetadataDocument>();
            string appData = Path.Combine(_projectRoot, "App_Data");
            if (Directory.Exists(appData))
            {
                foreach (string path in Directory.GetFiles(appData, "*.metadata.json", SearchOption.AllDirectories).OrderBy(p => p))
                {
                    if (!EsSidecarIndexable(path))
                        continue;

                    PageMetadataDocument doc = LoadPageMetadata(path);
                    if (doc != null && doc.PageMetadata != null)
                        result.Add(doc);
                }
            }

            if (result.Count > 0)
                return result;

            return LoadLegacyRootMetadata();
        }

        public void SaveAll(List<PageMetadataDocument> entries)
        {
            entries = entries ?? new List<PageMetadataDocument>();
            ResolveDuplicates(entries);

            foreach (var entry in entries)
                SavePageMetadata(entry);

            SaveRootIndex(entries);
        }

        public void SavePageMetadata(PageMetadataDocument entry)
        {
            if (entry == null || entry.PageMetadata == null || string.IsNullOrWhiteSpace(entry.PageMetadata.File))
                return;

            string absoluteTextPath = Path.Combine(_projectRoot, entry.PageMetadata.File.Replace('/', Path.DirectorySeparatorChar));
            string metadataPath = GetSidecarPath(absoluteTextPath);
            string json = JsonConvert.SerializeObject(entry, Formatting.Indented);
            WriteJsonAtomic(metadataPath, json);
        }

        private void SaveRootIndex(List<PageMetadataDocument> entries)
        {
            var rootEntries = entries
                .Where(e => e != null && e.PageMetadata != null && e.DomainMetadata != null)
                .OrderBy(e => e.PageMetadata.File)
                .Select(ToRootEntry)
                .ToList();

            string json = JsonConvert.SerializeObject(rootEntries, Formatting.Indented);
            WriteJsonAtomic(_metadataPath, json);
        }

        private RootMetadataEntry ToRootEntry(PageMetadataDocument entry)
        {
            var resource = entry.DomainMetadata;
            var page = entry.PageMetadata;

            return new RootMetadataEntry
            {
                Domain = resource.Domain,
                DomainSlug = resource.DomainSlug,
                Url = page.Url,
                File = page.File,
                SourceName = resource.SourceName,
                SourceType = resource.SourceType,
                Category = resource.Category,
                Region = resource.Region,
                Island = resource.Island,
                Language = resource.Language,
                License = resource.License,
                Topics = resource.Topics == null ? new List<string>() : new List<string>(resource.Topics),
                Description = resource.Description
            };
        }

        private PageMetadataDocument LoadPageMetadata(string path)
        {
            try
            {
                string json = System.IO.File.ReadAllText(path, Encoding.UTF8);
                return JsonConvert.DeserializeObject<PageMetadataDocument>(json);
            }
            catch
            {
                return null;
            }
        }

        private List<PageMetadataDocument> LoadLegacyRootMetadata()
        {
            if (!System.IO.File.Exists(_metadataPath))
                return new List<PageMetadataDocument>();

            try
            {
                string json = System.IO.File.ReadAllText(_metadataPath, Encoding.UTF8);
                var array = JArray.Parse(json);
                var result = new List<PageMetadataDocument>();

                foreach (JToken token in array)
                {
                    PageMetadataDocument doc = ConvertLegacyEntry(token as JObject);
                    if (doc != null)
                        result.Add(doc);
                }

                return result;
            }
            catch
            {
                return new List<PageMetadataDocument>();
            }
        }

        private PageMetadataDocument ConvertLegacyEntry(JObject item)
        {
            if (item == null)
                return null;

            string domain = Value(item, "domain");
            var manual = item["manual"] as JObject;

            var page = new PageMetadata
            {
                Job = Value(item, "job"),
                Domain = DomainToHost(domain),
                DomainSlug = Value(item, "domain_slug") ?? domain,
                PageNumber = ValueInt(item, "page_number"),
                Url = Value(manual, "url"),
                Title = Value(manual, "title"),
                File = Value(item, "file"),
                CrawledAt = Value(item, "crawled_at"),
                Chars = ValueInt(item, "chars"),
                Sha256 = Value(item, "sha256"),
                Quality = Value(item, "quality"),
                DuplicateOf = Value(item, "duplicate_of"),
                Depth = ValueInt(item, "depth")
            };

            if (string.IsNullOrWhiteSpace(page.Url))
                page.Url = ReconstructUrl(domain, Path.GetFileName(page.File ?? string.Empty));

            if (string.IsNullOrWhiteSpace(page.Title))
                page.Title = string.Format("(auto) {0} - pagina {1}", DomainToHost(domain), page.PageNumber);

            if (page.Depth == 0)
                page.Depth = InferDepthFromUrl(page.Url);

            return new PageMetadataDocument
            {
                DomainMetadata = BuildResourceMetadata(domain),
                PageMetadata = page
            };
        }

        private ResourceMetadata BuildResourceMetadata(string domain)
        {
            DomainMetadataSeed seed;
            if (!_domainMap.TryGetValue(domain ?? string.Empty, out seed))
            {
                seed = new DomainMetadataSeed
                {
                    SourceName = BuildSourceName(domain),
                    SourceType = "sitio web",
                    Category = "general",
                    Region = "Canarias",
                    Island = InferIsland(domain),
                    License = "desconocida",
                    Reliability = "media",
                    Notes = "Metadatos manuales estimados a partir del dominio.",
                    Description = string.Format("Recurso web relacionado con {0}.", DomainToHost(domain ?? "unknown")),
                    Topics = InferTopics(domain).ToArray()
                };
            }

            return new ResourceMetadata
            {
                Domain = DomainToHost(domain),
                DomainSlug = domain,
                SourceName = seed.SourceName,
                SourceType = seed.SourceType,
                Category = NormalizeSpanishText(seed.Category),
                Region = seed.Region,
                Island = seed.Island,
                Language = "es",
                License = seed.License,
                Topics = seed.Topics == null ? new List<string>() : seed.Topics.Select(NormalizeSpanishText).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Description = NormalizeSpanishText(seed.Description),
                Reliability = seed.Reliability,
                Notes = NormalizeSpanishText(seed.Notes)
            };
        }

        private int LoadExistingDepth(string absoluteFilePath, string url)
        {
            try
            {
                string sidecarPath = GetSidecarPath(absoluteFilePath);
                if (System.IO.File.Exists(sidecarPath))
                {
                    PageMetadataDocument existing = LoadPageMetadata(sidecarPath);
                    if (existing != null && existing.PageMetadata != null && existing.PageMetadata.Depth > 0)
                        return existing.PageMetadata.Depth;
                }
            }
            catch
            {
            }

            return InferDepthFromUrl(url);
        }

        private void WriteJsonAtomic(string path, string json)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string tmp = path + ".tmp";
            System.IO.File.WriteAllText(tmp, json, new UTF8Encoding(false));

            if (System.IO.File.Exists(path))
                System.IO.File.Replace(tmp, path, null);
            else
                System.IO.File.Move(tmp, path);
        }

        private bool EsSidecarIndexable(string metadataPath)
        {
            if (string.IsNullOrWhiteSpace(metadataPath))
                return false;

            string normalizada = metadataPath.Replace('\\', '/');
            if (normalizada.IndexOf("/debug_raw_html/", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            string textPath = GetTextPathFromSidecar(metadataPath);
            if (string.IsNullOrWhiteSpace(textPath))
                return false;

            string nombre = Path.GetFileName(textPath);
            if (nombre.IndexOf(".pre.", StringComparison.OrdinalIgnoreCase) >= 0 ||
                nombre.IndexOf(".final.", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return System.IO.File.Exists(textPath);
        }

        private static string GetTextPathFromSidecar(string metadataPath)
        {
            if (string.IsNullOrWhiteSpace(metadataPath) ||
                !metadataPath.EndsWith(".metadata.json", StringComparison.OrdinalIgnoreCase))
                return null;

            return metadataPath.Substring(0, metadataPath.Length - ".metadata.json".Length) + ".txt";
        }

        private static bool SameFile(PageMetadataDocument a, PageMetadataDocument b)
        {
            return a != null && b != null &&
                   a.PageMetadata != null && b.PageMetadata != null &&
                   string.Equals(a.PageMetadata.File, b.PageMetadata.File, StringComparison.OrdinalIgnoreCase);
        }

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

        private static string GetSidecarPath(string absoluteTextPath)
        {
            string directory = Path.GetDirectoryName(absoluteTextPath);
            string fileName = Path.GetFileNameWithoutExtension(absoluteTextPath) + ".metadata.json";
            return Path.Combine(directory, fileName);
        }

        private static string ExtractDomainFromPath(string relFile)
        {
            string[] parts = relFile.Replace('\\', '/').Split('/');
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
            string host = DomainToHost(domain);
            string path = filename ?? string.Empty;
            int underscore = path.IndexOf('_');
            if (underscore >= 0)
                path = path.Substring(underscore + 1);
            if (path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                path = path.Substring(0, path.Length - 4);

            path = path.Replace('_', '/').Trim('/');
            return path == "home" || string.IsNullOrWhiteSpace(path)
                ? string.Format("https://{0}/", host)
                : string.Format("https://{0}/{1}", host, path);
        }

        private static string DomainToHost(string domain)
        {
            return (domain ?? "unknown").Replace('_', '.');
        }

        private static int InferDepthFromUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                return 0;

            string path = (uri.AbsolutePath ?? string.Empty).Trim('/');
            if (string.IsNullOrWhiteSpace(path))
                return 0;

            return path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private static string ExtractFirstLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "(auto) sin titulo";

            foreach (string line in text.Split('\n'))
            {
                string trimmed = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    return trimmed.Length > 120 ? trimmed.Substring(0, 120) : trimmed;
            }
            return "(auto) sin titulo";
        }

        private static string BuildSourceName(string domain)
        {
            string host = DomainToHost(domain);
            string[] parts = host.Split('.');
            if (parts.Length == 0)
                return "Recurso web";

            return string.Join(" ", parts
                .Where(p => p.Length > 2 && !string.Equals(p, "www", StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .Select(Capitalize));
        }

        private static string Capitalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        private static string InferIsland(string domain)
        {
            string lower = (domain ?? string.Empty).ToLowerInvariant();
            if (lower.Contains("lanzarote"))
                return "Lanzarote";
            if (lower.Contains("grancanaria") || lower.Contains("museocanario"))
                return "Gran Canaria";
            return "Canarias";
        }

        private static List<string> InferTopics(string domain)
        {
            var result = new List<string>();
            string lower = (domain ?? string.Empty).ToLowerInvariant();

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
                result.Add("general");

            return result;
        }

        private static string NormalizeSpanishText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return value
                .Replace("historica", "histórica")
                .Replace("Historica", "Histórica")
                .Replace("historico", "histórico")
                .Replace("Historico", "Histórico")
                .Replace("documentacion", "documentación")
                .Replace("Documentacion", "Documentación")
                .Replace("museistico", "museístico")
                .Replace("Museistico", "Museístico")
                .Replace("arqueologia", "arqueología")
                .Replace("Arqueologia", "Arqueología")
                .Replace("linguistica", "lingüística")
                .Replace("Linguistica", "Lingüística")
                .Replace("institucion", "institución")
                .Replace("Institucion", "Institución")
                .Replace("academica", "académica")
                .Replace("Academica", "Académica");
        }

        private static string Value(JObject obj, string propertyName)
        {
            if (obj == null)
                return null;

            JToken token = obj[propertyName];
            return token == null || token.Type == JTokenType.Null ? null : token.ToString();
        }

        private static int ValueInt(JObject obj, string propertyName)
        {
            if (obj == null)
                return 0;

            int value;
            return int.TryParse(Value(obj, propertyName), out value) ? value : 0;
        }
    }
}
