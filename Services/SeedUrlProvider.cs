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

            var urls = new List<string>();
            var errores = new List<string>();

            foreach (var linea in File.ReadAllLines(rutaSemillas))
            {
                string valor = (linea ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(valor) || valor.StartsWith("#"))
                    continue;

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
                FilePath = rutaSemillas,
                Urls = urls,
                InvalidEntries = errores
            };
        }

        private string ResolverRutaSemillas(string rutaConfigurada)
        {
            if (string.IsNullOrWhiteSpace(rutaConfigurada))
                return HostingEnvironment.MapPath("~/App_Data/seeds.txt");

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
    }
}
