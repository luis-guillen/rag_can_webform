using System;
using System.Collections.Generic;
using System.IO;
using System.Web.UI;
using rag_can_aspx.Services;

namespace rag_can_aspx
{
    public partial class _Default : Page
    {
        private readonly string[] _seedUrls = new[]
        {
            "https://elmuseocanario.com/",
            "https://cultura.grancanaria.com/museos",
            "https://memoriadelanzarote.com/",
            "https://canarias-azul.iatext.ulpgc.es/",
            "https://izuran.blogspot.com/",
            "https://www.academiacanarialengua.org/diccionario/"
        };

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void BtnCrawl_Click(object sender, EventArgs e)
        {
            try
            {
                lblError.Style["display"] = "none";

                string url = (txtUrl.Text ?? "").Trim();

                if (!int.TryParse(txtMaxPages.Text, out int maxPages) || maxPages < 1 || maxPages > 10000)
                {
                    MostrarError("Max Páginas debe ser un número entre 1 y 10000.");
                    return;
                }

                if (!int.TryParse(txtMaxDepth.Text, out int maxDepth) || maxDepth < 0 || maxDepth > 10)
                {
                    MostrarError("Max Profundidad debe ser un número entre 0 y 10.");
                    return;
                }

                int maxPaginasPorSitio = chkFullCrawl.Checked ? 1000 : maxPages;

                var seeds = new List<string>();
                if (!string.IsNullOrWhiteSpace(url))
                {
                    if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                    {
                        MostrarError("La URL introducida no es válida.");
                        return;
                    }
                    seeds.Add(url);
                }
                else
                {
                    seeds.AddRange(_seedUrls);
                }

                string appBase = Server.MapPath("~/App_Data/");
                string carpetaBase = PathHelper.ResolverRutaCarpeta(appBase, txtCarpeta.Text);

                var resultados = new List<string>();
                var crawler = new CrawlerService();

                foreach (var seed in seeds)
                {
                    try
                    {
                        if (!Uri.TryCreate(seed, UriKind.Absolute, out Uri startUri))
                        {
                            resultados.Add($"URL inválida: {seed}");
                            continue;
                        }

                        string nombreCarpeta = crawler.GenerarNombreCarpetaDominio(startUri);
                        string carpetaSitio = Path.Combine(carpetaBase, nombreCarpeta);

                        var resultado = System.Threading.Tasks.Task
                            .Run(() => crawler.CrawlDominio(seed, carpetaSitio, maxPaginasPorSitio, maxDepth))
                            .Result;

                        string rutaRel = PathHelper.ObtenerRutaRelativa(Server.MapPath("~"), carpetaSitio);
                        if (resultado.Exitoso && resultado.PaginasDescargadas > 0)
                            resultados.Add($"✓ {startUri.Host} → {resultado.PaginasDescargadas} páginas en {rutaRel}");
                        else if (resultado.Exitoso && resultado.PaginasDescargadas == 0)
                            resultados.Add($"⚠ {startUri.Host} → {resultado.Mensaje}");
                        else
                            resultados.Add($"✗ {startUri.Host} → ERROR: {resultado.Mensaje}");
                    }
                    catch (Exception ex)
                    {
                        resultados.Add($"✗ {seed} → ERROR: {ex.Message}");
                    }
                }

                Session["Resultados"] = resultados;
                Session["CarpetaBase"] = PathHelper.ObtenerRutaRelativa(Server.MapPath("~"), carpetaBase);
                Response.Redirect("~/Resultados.aspx", false);
            }
            catch (Exception ex)
            {
                MostrarError($"Error inesperado: {ex.Message}");
            }
        }

        private void MostrarError(string mensaje)
        {
            lblError.Text = mensaje;
            lblError.Style["display"] = "block";
        }
    }
}