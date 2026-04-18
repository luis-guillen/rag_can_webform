using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using rag_can_aspx.Services;

namespace rag_can_aspx
{
    public partial class Resultados : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
                return;

            string jobId = (Request.QueryString["jobId"] ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(jobId))
            {
                MostrarError("No se ha indicado ningún trabajo.");
                return;
            }

            var job = CrawlJobManager.GetJob(jobId);
            if (job == null)
            {
                MostrarError("El trabajo no existe o ya expiró.");
                return;
            }

            if (job.Status == CrawlJobState.Queued || job.Status == CrawlJobState.Running)
            {
                Response.Headers["Refresh"] = "3";
            }

            MostrarEstado(job);
        }

        private void MostrarEstado(CrawlJobInfo job)
        {
            phSuccess.Visible = true;
            phError.Visible = false;

            string resumen = $"Job {job.JobId} · {TraducirEstado(job.Status)} · " +
                             $"{job.Domains.Count(d => d.Status == CrawlJobState.Completed)}/{job.Domains.Count} dominios completados";

            litCarpeta.Text = HttpUtility.HtmlEncode($"{job.BaseFolderRelative} ({resumen})");
            litResultados.Text = ConstruirHtmlResultados(job);
        }

        private string ConstruirHtmlResultados(CrawlJobInfo job)
        {
            var sb = new StringBuilder();

            foreach (var domain in job.Domains.OrderBy(d => d.Host, StringComparer.OrdinalIgnoreCase))
            {
                string borderColor = ObtenerColor(domain.Status);
                string ruta = string.IsNullOrWhiteSpace(domain.OutputFolderRelative)
                    ? string.Empty
                    : $"<div><strong>Carpeta:</strong> <code>{HttpUtility.HtmlEncode(domain.OutputFolderRelative)}</code></div>";

                string paginas = domain.PagesDownloaded > 0
                    ? $"<div><strong>Páginas:</strong> {domain.PagesDownloaded}</div>"
                    : string.Empty;

                string mensaje = string.IsNullOrWhiteSpace(domain.Message)
                    ? string.Empty
                    : $"<div>{HttpUtility.HtmlEncode(domain.Message)}</div>";

                sb.AppendLine(
                    $"<li style=\"padding: 12px; margin-bottom: 10px; background-color: #f5f5f5; border-left: 4px solid {borderColor}; border-radius: 2px;\">" +
                    $"<div><strong>{HttpUtility.HtmlEncode(domain.Host)}</strong> · {HttpUtility.HtmlEncode(TraducirEstado(domain.Status))}</div>" +
                    $"<div style=\"margin-top: 6px;\"><small>{HttpUtility.HtmlEncode(domain.SeedUrl)}</small></div>" +
                    $"{paginas}{ruta}{mensaje}</li>");
            }

            if (!string.IsNullOrWhiteSpace(job.ErrorMessage))
            {
                sb.AppendLine(
                    $"<li style=\"padding: 12px; margin-bottom: 10px; background-color: #fff3cd; border-left: 4px solid #d39e00; border-radius: 2px;\">" +
                    $"<strong>Error del job:</strong> {HttpUtility.HtmlEncode(job.ErrorMessage)}</li>");
            }

            return sb.ToString();
        }

        private string ObtenerColor(CrawlJobState state)
        {
            switch (state)
            {
                case CrawlJobState.Completed:
                    return "#4caf50";
                case CrawlJobState.Failed:
                    return "#dc3545";
                case CrawlJobState.Running:
                    return "#0d6efd";
                default:
                    return "#6c757d";
            }
        }

        private string TraducirEstado(CrawlJobState state)
        {
            switch (state)
            {
                case CrawlJobState.Queued:
                    return "En cola";
                case CrawlJobState.Running:
                    return "En ejecución";
                case CrawlJobState.Completed:
                    return "Completado";
                case CrawlJobState.Failed:
                    return "Fallido";
                default:
                    return state.ToString();
            }
        }

        private void MostrarError(string mensaje)
        {
            phError.Visible = true;
            phSuccess.Visible = false;
            litError.Text = $"<p>{HttpUtility.HtmlEncode(mensaje)}</p>";
        }
    }
}
