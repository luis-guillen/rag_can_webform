using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;

namespace rag_can_aspx.Services
{
    public sealed class SeedUrlProvider
    {
        private readonly CrawlerSettings _settings;

        public SeedUrlProvider(CrawlerSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public SeedLoadResult Load()
        {
            string rutaSemillas = ResolverRutaSemillas(_settings.SeedsFile);
            if (string.IsNullOrWhiteSpace(rutaSemillas) || !File.Exists(rutaSemillas))
            {
                throw new FileNotFoundException(
                    $"No se encontró el archivo de semillas configurado: {_settings.SeedsFile}",
                    rutaSemillas);
            }

            SeedLoadResult result = ParseLines(File.ReadAllLines(rutaSemillas));
            result.FilePath = rutaSemillas;
            return result;
        }

        public void SaveUrls(IEnumerable<string> urls)
        {
            if (urls == null)
                throw new ArgumentNullException(nameof(urls));

            string rutaSemillas = ResolverRutaSemillas(_settings.SeedsFile);
            string directory = Path.GetDirectoryName(rutaSemillas);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllLines(rutaSemillas, urls.ToArray());
        }

        public static SeedLoadResult ParseLines(IEnumerable<string> lines)
        {
            var urls = new List<string>();
            var errores = new List<string>();
            int entradas = 0;

            if (lines == null)
            {
                return new SeedLoadResult
                {
                    Urls = urls,
                    InvalidEntries = errores,
                    EntryCount = entradas
                };
            }

            foreach (var linea in lines)
            {
                string valor = (linea ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(valor) || valor.StartsWith("#"))
                    continue;

                entradas++;

                Uri uri;
                if (!Uri.TryCreate(valor, UriKind.Absolute, out uri))
                {
                    errores.Add(valor);
                    continue;
                }

                urls.Add(uri.ToString());
            }

            urls = urls
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new SeedLoadResult
            {
                Urls = urls,
                InvalidEntries = errores,
                EntryCount = entradas
            };
        }

        private string ResolverRutaSemillas(string rutaConfigurada)
        {
            if (string.IsNullOrWhiteSpace(rutaConfigurada))
                return HostingEnvironment.MapPath("~/Config/seeds.txt");

            if (Path.IsPathRooted(rutaConfigurada))
                return rutaConfigurada;

            if (rutaConfigurada.StartsWith("~/", StringComparison.Ordinal))
                return HostingEnvironment.MapPath(rutaConfigurada);

            return HostingEnvironment.MapPath("~/" + rutaConfigurada.TrimStart('/'));
        }
    }

    public sealed class SeedLoadResult
    {
        public string FilePath { get; set; }
        public List<string> Urls { get; set; }
        public List<string> InvalidEntries { get; set; }
        public int EntryCount { get; set; }
    }
}
