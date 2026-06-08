using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using rag_can_aspx.Services;
using rag_can_aspx.Services.Jobs;

namespace rag_can_aspx
{
    public partial class Crawler : Page
    {
        private readonly CrawlerIndexerFacade _facade = new CrawlerIndexerFacade();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarScheduler();
                RefrescarVista();
            }
        }

        // -------- acciones --------

        protected void BtnIniciar_Click(object sender, EventArgs e)
        {
            int maxPages, maxDepth;
            if (!int.TryParse(txtMaxPages.Text, out maxPages) || maxPages < 1 || maxPages > 10000)
            {
                MostrarMensaje("Max Paginas debe estar entre 1 y 10000.", false);
                return;
            }
            if (!int.TryParse(txtMaxDepth.Text, out maxDepth) || maxDepth < 0 || maxDepth > 10)
            {
                MostrarMensaje("Max Profundidad debe estar entre 0 y 10.", false);
                return;
            }

            string url = (txtUrl.Text ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(url))
            {
                JobActionResult resultUrl = _facade.StartCrawlSource(url, maxPages, maxDepth);
                string mensaje = resultUrl.Message;
                if (fuSeeds.HasFile)
                    mensaje += " Se ignoro el archivo subido porque la URL unica tiene prioridad.";

                MostrarMensaje(mensaje, resultUrl.Accepted);
                RefrescarVista();
                ActualizarEstado();
                return;
            }

            JobActionResult result = fuSeeds.HasFile
                ? IniciarCrawlDesdeArchivo(maxPages, maxDepth)
                : _facade.StartCrawl(maxPages, maxDepth);

            MostrarMensaje(result.Message, result.Accepted);
            RefrescarVista();
            ActualizarEstado();
        }

        protected void BtnParar_Click(object sender, EventArgs e)
        {
            JobActionResult result = _facade.StopCrawl();
            MostrarMensaje(result.Message, result.Accepted);
            RefrescarVista();
            ActualizarEstado();
        }

        protected void TmrRefresco_Tick(object sender, EventArgs e)
        {
            RefrescarVista();
        }

        protected void BtnGuardarScheduler_Click(object sender, EventArgs e)
        {
            var cfg = JobStatusManager.ReadScheduler() ?? new SchedulerConfig();
            cfg.Mode = ddlMode.SelectedValue;
            cfg.CrawlEnabled = chkSchedCrawl.Checked;
            cfg.IndexEnabled = chkSchedIndex.Checked;

            int horas;
            if (int.TryParse(txtIntervalHours.Text, out horas) && horas >= 1 && horas <= 720)
                cfg.IntervalHours = horas;

            TimeSpan tod;
            if (TimeSpan.TryParse((txtDailyTime.Text ?? string.Empty).Trim(), out tod))
                cfg.DailyTime = string.Format("{0:D2}:{1:D2}", tod.Hours, tod.Minutes);

            JobStatusManager.WriteScheduler(cfg);

            lblScheduler.Text = "Programacion guardada (modo: " + HttpUtility.HtmlEncode(cfg.Mode) + ").";
            lblScheduler.Visible = true;
        }

        // -------- render --------

        private void ActualizarEstado()
        {
            var sm = ScriptManager.GetCurrent(Page);
            if (sm != null && sm.IsInAsyncPostBack)
                updEstado.Update();
        }

        private void RefrescarVista()
        {
            JobRunStatus status = _facade.GetCrawlStatus();
            litEstado.Text = ConstruirEstadoHtml(status);
            litFuentes.Text = ConstruirFuentesHtml(_facade.GetSources());
            litLogs.Text = ConstruirLogsHtml(_facade.GetLogs(40).Crawler);
        }

        private void CargarScheduler()
        {
            SchedulerConfig cfg = JobStatusManager.ReadScheduler() ?? new SchedulerConfig();
            SeleccionarDropdown(ddlMode, cfg.Mode);
            chkSchedCrawl.Checked = cfg.CrawlEnabled;
            chkSchedIndex.Checked = cfg.IndexEnabled;
            txtIntervalHours.Text = cfg.IntervalHours.ToString();
            txtDailyTime.Text = string.IsNullOrWhiteSpace(cfg.DailyTime) ? "03:00" : cfg.DailyTime;
        }

        private static void SeleccionarDropdown(System.Web.UI.WebControls.DropDownList ddl, string value)
        {
            var item = ddl.Items.FindByValue(value ?? "manual");
            if (item != null)
            {
                ddl.ClearSelection();
                item.Selected = true;
            }
        }

        private void MostrarMensaje(string mensaje, bool ok)
        {
            lblMensaje.Text = HttpUtility.HtmlEncode(mensaje ?? string.Empty).Replace("\n", "<br />");
            lblMensaje.CssClass = ok ? "alert alert-info d-block mb-3" : "alert alert-danger d-block mb-3";
            lblMensaje.Visible = true;
        }

        private JobActionResult IniciarCrawlDesdeArchivo(int maxPages, int maxDepth)
        {
            if (fuSeeds.PostedFile == null || fuSeeds.PostedFile.ContentLength == 0)
                return JobActionResult.Fail("Archivo vacio.");

            string extension = Path.GetExtension(fuSeeds.FileName);
            if (!string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase))
                return JobActionResult.Fail("El archivo debe ser .txt.");

            SeedLoadResult seeds;
            try
            {
                using (var reader = new StreamReader(fuSeeds.FileContent))
                {
                    seeds = SeedUrlProvider.ParseLines(LeerLineas(reader));
                }
            }
            catch (Exception ex)
            {
                return JobActionResult.Fail("No se pudo leer el archivo: " + ex.GetBaseException().Message);
            }

            if (seeds.InvalidEntries != null && seeds.InvalidEntries.Count > 0)
                return JobActionResult.Fail("URLs invalidas detectadas:\n" + string.Join("\n", seeds.InvalidEntries));

            if (seeds.EntryCount == 0)
                return JobActionResult.Fail("Archivo sin URLs validas.");

            if (seeds.Urls == null || seeds.Urls.Count == 0)
                return JobActionResult.Fail("Archivo sin URLs validas.");

            bool guardar = string.Equals(rblSeedFileMode.SelectedValue, "saveAndUse", StringComparison.OrdinalIgnoreCase);
            if (guardar)
            {
                try
                {
                    var provider = new SeedUrlProvider(CrawlerSettings.Load());
                    provider.SaveUrls(seeds.Urls);
                }
                catch (Exception ex)
                {
                    return JobActionResult.Fail("No se pudo guardar el archivo de semillas: " + ex.GetBaseException().Message);
                }
            }

            JobActionResult result = _facade.StartCrawlSources(seeds.Urls, maxPages, maxDepth);
            if (!result.Accepted)
                return result;

            string prefijo = guardar
                ? "Archivo guardado e inicio de crawl aceptado"
                : "Archivo aceptado para este crawl";

            return JobActionResult.Ok(prefijo + " (" + seeds.Urls.Count + " URL(s)). " + result.Message);
        }

        private static List<string> LeerLineas(StreamReader reader)
        {
            var lines = new List<string>();
            string line;
            while ((line = reader.ReadLine()) != null)
                lines.Add(line);
            return lines;
        }

        private string ConstruirEstadoHtml(JobRunStatus s)
        {
            if (s == null)
                return "<p class=\"text-muted\">Sin datos de estado todavia.</p>";

            int pct = Math.Max(0, Math.Min(100, s.ProgressPercent));
            string color = ColorEstado(s.State);
            string label = TraducirEstado(s.State);

            var sb = new StringBuilder();
            sb.Append("<div class=\"mb-2\"><span class=\"badge\" style=\"background-color:" + color + ";\">" + HttpUtility.HtmlEncode(label) + "</span></div>");
            sb.Append("<div class=\"progress mb-3\" style=\"height:22px;\">");
            sb.Append("<div class=\"progress-bar\" role=\"progressbar\" style=\"width:" + pct + "%;\">" + pct + "%</div>");
            sb.Append("</div>");

            sb.Append("<ul class=\"list-group\">");
            sb.Append(Item("Fuentes totales", s.TotalSources.ToString()));
            sb.Append(Item("Procesadas (con cambios)", s.ProcessedSources.ToString()));
            sb.Append(Item("Sin cambios (skipped)", s.SkippedSources.ToString()));
            sb.Append(Item("Fallidas", s.FailedSources.ToString()));
            if (!string.IsNullOrWhiteSpace(s.CurrentUrl))
                sb.Append(Item("URL actual", s.CurrentUrl));
            if (!string.IsNullOrWhiteSpace(s.StartedAt))
                sb.Append(Item("Inicio (UTC)", s.StartedAt));
            if (!string.IsNullOrWhiteSpace(s.FinishedAt))
                sb.Append(Item("Fin (UTC)", s.FinishedAt));
            if (!string.IsNullOrWhiteSpace(s.LastError))
                sb.Append("<li class=\"list-group-item text-danger\"><strong>Ultimo error:</strong> " + HttpUtility.HtmlEncode(s.LastError) + "</li>");
            sb.Append("</ul>");

            return sb.ToString();
        }

        private static string Item(string label, string value)
        {
            return "<li class=\"list-group-item\"><strong>" + HttpUtility.HtmlEncode(label) + ":</strong> " + HttpUtility.HtmlEncode(value ?? string.Empty) + "</li>";
        }

        private string ConstruirFuentesHtml(List<SourceStatus> sources)
        {
            if (sources == null || sources.Count == 0)
                return "<p class=\"text-muted\">Aun no hay fuentes registradas. Lanza un crawl.</p>";

            var sb = new StringBuilder();
            sb.Append("<div class=\"table-responsive\"><table class=\"table table-sm table-striped align-middle\">");
            sb.Append("<thead><tr><th>Host</th><th>Estado</th><th>Paginas</th><th>Cambiadas</th><th>needs_index</th><th>Chunks</th><th>Ultimo crawl (UTC)</th></tr></thead><tbody>");
            foreach (SourceStatus src in sources)
            {
                sb.Append("<tr>");
                sb.Append("<td>" + HttpUtility.HtmlEncode(src.Host ?? src.Url) + "</td>");
                sb.Append("<td><span class=\"badge\" style=\"background-color:" + ColorFuente(src.State) + ";\">" + HttpUtility.HtmlEncode(src.State ?? "-") + "</span></td>");
                sb.Append("<td>" + src.PagesTotal + "</td>");
                sb.Append("<td>" + src.PagesChanged + "</td>");
                sb.Append("<td>" + (src.NeedsIndex ? "si" : "no") + "</td>");
                sb.Append("<td>" + src.ChunkCount + "</td>");
                sb.Append("<td><small>" + HttpUtility.HtmlEncode(src.LastCrawledAt ?? "-") + "</small></td>");
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table></div>");
            return sb.ToString();
        }

        private string ConstruirLogsHtml(List<string> lines)
        {
            if (lines == null || lines.Count == 0)
                return "<p class=\"text-muted\">Sin logs todavia.</p>";

            var sb = new StringBuilder();
            sb.Append("<pre style=\"max-height:280px; overflow:auto; background:#1e1e1e; color:#d4d4d4; padding:12px; border-radius:6px;\">");
            foreach (string line in lines)
                sb.Append(HttpUtility.HtmlEncode(line) + "\n");
            sb.Append("</pre>");
            return sb.ToString();
        }

        private static string TraducirEstado(string state)
        {
            switch (state)
            {
                case JobStates.Idle: return "Inactivo";
                case JobStates.Running: return "En ejecucion";
                case JobStates.Completed: return "Completado";
                case JobStates.Error: return "Error";
                case JobStates.Stopped: return "Detenido";
                default: return state ?? "-";
            }
        }

        private static string ColorEstado(string state)
        {
            switch (state)
            {
                case JobStates.Running: return "#0d6efd";
                case JobStates.Completed: return "#198754";
                case JobStates.Error: return "#dc3545";
                case JobStates.Stopped: return "#fd7e14";
                default: return "#6c757d";
            }
        }

        private static string ColorFuente(string state)
        {
            switch (state)
            {
                case "ok": return "#198754";
                case "skipped": return "#6c757d";
                case "failed": return "#dc3545";
                default: return "#6c757d";
            }
        }
    }
}
