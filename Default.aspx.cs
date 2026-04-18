using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI;
using rag_can_aspx.Services;

namespace rag_can_aspx
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void BtnCrawl_Click(object sender, EventArgs e)
        {
            try
            {
                lblError.Style["display"] = "none";

                string url = (txtUrl.Text ?? string.Empty).Trim();
                int maxPages;
                int maxDepth;

                if (!int.TryParse(txtMaxPages.Text, out maxPages) || maxPages < 1 || maxPages > 10000)
                {
                    MostrarError("Max Páginas debe ser un número entre 1 y 10000.");
                    return;
                }

                if (!int.TryParse(txtMaxDepth.Text, out maxDepth) || maxDepth < 0 || maxDepth > 10)
                {
                    MostrarError("Max Profundidad debe ser un número entre 0 y 10.");
                    return;
                }

                int maxPaginasPorSitio = chkFullCrawl.Checked ? 1000 : maxPages;
                var settings = CrawlerSettings.Load();
                var seeds = ResolverSemillas(url, settings);

                if (seeds.Count == 0)
                {
                    MostrarError("No hay URLs válidas para procesar.");
                    return;
                }

                string appBase = Server.MapPath("~/App_Data/");
                string carpetaBase = PathHelper.ResolverRutaCarpeta(appBase, txtCarpeta.Text);

                var job = CrawlJobManager.QueueJob(
                    new CrawlJobRequest
                    {
                        SeedUrls = seeds,
                        BaseFolderAbsolute = carpetaBase,
                        MaxPages = maxPaginasPorSitio,
                        MaxDepth = maxDepth,
                        Settings = settings
                    },
                    Server.MapPath("~"));

                Response.Redirect("~/Resultados.aspx?jobId=" + job.JobId, false);
            }
            catch (Exception ex)
            {
                MostrarError($"Error inesperado: {ex.Message}");
            }
        }

        private List<string> ResolverSemillas(string urlManual, CrawlerSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(urlManual))
            {
                Uri parsed;
                if (!Uri.TryCreate(urlManual, UriKind.Absolute, out parsed))
                {
                    throw new InvalidOperationException("La URL introducida no es válida.");
                }

                return new List<string> { parsed.ToString() };
            }

            var provider = new SeedUrlProvider(settings);
            var result = provider.Load();

            if (result.InvalidEntries.Any())
            {
                string invalidas = string.Join(", ", result.InvalidEntries.Take(5));
                throw new InvalidOperationException(
                    $"El archivo de semillas contiene URLs inválidas: {invalidas}");
            }

            return result.Urls;
        }

        private void MostrarError(string mensaje)
        {
            lblError.Text = mensaje;
            lblError.Style["display"] = "block";
        }
    }
}
