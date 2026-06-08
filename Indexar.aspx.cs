using rag_can_aspx.Services;
using rag_can_aspx.Services.Jobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;

namespace rag_can_aspx
{
    public partial class Indexar : Page
    {
        private readonly CrawlerIndexerFacade _facade = new CrawlerIndexerFacade();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarDropdown();
                RefrescarIndex();
            }
        }

        // -------- indexado incremental en background --------

        protected void BtnIniciarIndex_Click(object sender, EventArgs e)
        {
            JobActionResult result = _facade.StartIndexing();
            MostrarIndexMsg(result.Message, result.Accepted);
            RefrescarIndex();
            ActualizarIndexEstado();
        }

        protected void BtnPararIndex_Click(object sender, EventArgs e)
        {
            JobActionResult result = _facade.StopIndexing();
            MostrarIndexMsg(result.Message, result.Accepted);
            RefrescarIndex();
            ActualizarIndexEstado();
        }

        protected void TmrIndex_Tick(object sender, EventArgs e)
        {
            RefrescarIndex();
        }

        private void ActualizarIndexEstado()
        {
            var sm = ScriptManager.GetCurrent(Page);
            if (sm != null && sm.IsInAsyncPostBack)
                updIndexEstado.Update();
        }

        private void RefrescarIndex()
        {
            JobRunStatus status = _facade.GetIndexingStatus();
            litIndexEstado.Text = ConstruirIndexEstadoHtml(status);
            litIndexLogs.Text = ConstruirLogsHtml(_facade.GetLogs(40).Indexer);
        }

        private void MostrarIndexMsg(string mensaje, bool ok)
        {
            lblIndexMsg.Text = System.Web.HttpUtility.HtmlEncode(mensaje ?? string.Empty);
            lblIndexMsg.CssClass = ok ? "alert alert-info d-block mb-3" : "alert alert-danger d-block mb-3";
            lblIndexMsg.Visible = true;
        }

        private string ConstruirIndexEstadoHtml(JobRunStatus s)
        {
            if (s == null)
                return "<p class=\"text-muted\">Sin datos de indexado todavia.</p>";

            int pct = Math.Max(0, Math.Min(100, s.ProgressPercent));
            var sb = new StringBuilder();
            sb.Append("<div class=\"mb-2\"><span class=\"badge bg-secondary\">" + System.Web.HttpUtility.HtmlEncode(TraducirEstado(s.State)) + "</span></div>");
            sb.Append("<div class=\"progress mb-3\" style=\"height:22px;\"><div class=\"progress-bar\" role=\"progressbar\" style=\"width:" + pct + "%;\">" + pct + "%</div></div>");
            sb.Append("<ul class=\"list-group\">");
            sb.Append("<li class=\"list-group-item\"><strong>Documentos pendientes:</strong> " + s.TotalSources + "</li>");
            sb.Append("<li class=\"list-group-item text-success\"><strong>Indexados:</strong> " + s.ProcessedSources + "</li>");
            sb.Append("<li class=\"list-group-item text-danger\"><strong>Con fallo:</strong> " + s.FailedSources + "</li>");
            if (!string.IsNullOrWhiteSpace(s.CurrentUrl))
                sb.Append("<li class=\"list-group-item\"><strong>Documento actual:</strong> " + System.Web.HttpUtility.HtmlEncode(s.CurrentUrl) + "</li>");
            if (!string.IsNullOrWhiteSpace(s.FinishedAt))
                sb.Append("<li class=\"list-group-item\"><strong>Fin (UTC):</strong> " + System.Web.HttpUtility.HtmlEncode(s.FinishedAt) + "</li>");
            if (!string.IsNullOrWhiteSpace(s.LastError))
                sb.Append("<li class=\"list-group-item text-danger\"><strong>Ultimo error:</strong> " + System.Web.HttpUtility.HtmlEncode(s.LastError) + "</li>");
            sb.Append("</ul>");
            return sb.ToString();
        }

        private string ConstruirLogsHtml(List<string> lines)
        {
            if (lines == null || lines.Count == 0)
                return "<p class=\"text-muted\">Sin logs todavia.</p>";

            var sb = new StringBuilder();
            sb.Append("<pre style=\"max-height:260px; overflow:auto; background:#1e1e1e; color:#d4d4d4; padding:12px; border-radius:6px;\">");
            foreach (string line in lines)
                sb.Append(System.Web.HttpUtility.HtmlEncode(line) + "\n");
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

        private void CargarDropdown()
        {
            ddlCarpeta.Items.Clear();
            ddlCarpeta.Items.Add(new System.Web.UI.WebControls.ListItem("-- Selecciona una carpeta --", ""));

            string appData = Server.MapPath("~/App_Data/");
            if (!Directory.Exists(appData))
                return;

            foreach (string dir in Directory.GetDirectories(appData).OrderBy(d => d))
            {
                string name = Path.GetFileName(dir);
                ddlCarpeta.Items.Add(new System.Web.UI.WebControls.ListItem(name, name));
            }
        }

        protected void BtnIndexar_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;
            phResumen.Visible = false;

            string appData = Server.MapPath("~/App_Data/");
            string carpetaElegida = txtCarpetaCustom.Text.Trim();
            if (string.IsNullOrWhiteSpace(carpetaElegida))
                carpetaElegida = ddlCarpeta.SelectedValue;

            if (string.IsNullOrWhiteSpace(carpetaElegida))
            {
                MostrarError("Selecciona una carpeta o introduce una ruta personalizada.");
                return;
            }

            string carpetaAbs;
            try
            {
                carpetaAbs = PathHelper.ResolverRutaCarpeta(appData, carpetaElegida).TrimEnd('\\', '/');
            }
            catch (ArgumentException ex)
            {
                MostrarError(ex.Message);
                return;
            }

            if (!Directory.Exists(carpetaAbs))
            {
                MostrarError($"La carpeta no existe: {carpetaElegida}");
                return;
            }

            string projectRoot = Server.MapPath("~");
            var svc = new MetadataService(projectRoot);
            string jobName = Path.GetFileName(carpetaAbs);

            SearchOption opcion = chkRecursivo.Checked
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            string[] archivos = Directory.GetFiles(carpetaAbs, "*.txt", opcion)
                .Where(EsArchivoIndexablePrimario)
                .ToArray();

            int total = 0, lowQuality = 0, empty = 0, bomLimpiados = 0;
            var nuevasEntradas = new List<PageMetadataDocument>();

            foreach (string archivo in archivos.OrderBy(f => f))
            {
                // Limpiar BOM en disco si lo tiene
                try
                {
                    byte[] bytes = File.ReadAllBytes(archivo);
                    if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                    {
                        File.WriteAllBytes(archivo, bytes.Skip(3).ToArray());
                        bomLimpiados++;
                    }
                }
                catch { }

                PageMetadataDocument meta = svc.BuildForExistingPage(archivo, jobName);
                nuevasEntradas.Add(meta);

                if (meta.PageMetadata.Quality == "empty") empty++;
                else if (meta.PageMetadata.Quality == "low_quality") lowQuality++;
                total++;
            }

            // Resolver duplicados dentro del lote + contra entradas existentes
            var todas = svc.LoadAll();
            // Quitar entradas previas de este job para reemplazarlas
            todas.RemoveAll(e2 => e2.PageMetadata != null &&
                                  string.Equals(e2.PageMetadata.Job, jobName, StringComparison.OrdinalIgnoreCase));
            todas.AddRange(nuevasEntradas);
            svc.ResolveDuplicates(todas);
            svc.SaveAll(todas);

            var archivosDelJob = new HashSet<string>(
                nuevasEntradas
                    .Where(m => m != null && m.PageMetadata != null && !string.IsNullOrWhiteSpace(m.PageMetadata.File))
                    .Select(m => m.PageMetadata.File),
                StringComparer.OrdinalIgnoreCase);

            int duplicadosInternos = nuevasEntradas.Count(m =>
                m != null &&
                m.PageMetadata != null &&
                !string.IsNullOrWhiteSpace(m.PageMetadata.DuplicateOf) &&
                archivosDelJob.Contains(m.PageMetadata.DuplicateOf));

            int duplicadosHistoricos = nuevasEntradas.Count(m =>
                m != null &&
                m.PageMetadata != null &&
                !string.IsNullOrWhiteSpace(m.PageMetadata.DuplicateOf) &&
                !archivosDelJob.Contains(m.PageMetadata.DuplicateOf));

            MostrarResumen(jobName, total, lowQuality, empty, bomLimpiados, duplicadosInternos, duplicadosHistoricos);
            CargarDropdown();
        }

        private void MostrarError(string mensaje)
        {
            lblError.Text = System.Web.HttpUtility.HtmlEncode(mensaje);
            lblError.Visible = true;
        }

        private static bool EsArchivoIndexablePrimario(string rutaArchivo)
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo))
                return false;

            string normalizada = rutaArchivo.Replace('\\', '/');
            if (normalizada.IndexOf("/debug_raw_html/", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            string nombre = Path.GetFileName(normalizada);
            if (nombre.IndexOf(".pre.", StringComparison.OrdinalIgnoreCase) >= 0 ||
                nombre.IndexOf(".final.", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return true;
        }

        private void MostrarResumen(string job, int total, int lowQuality, int empty, int bomLimpiados, int duplicadosInternos, int duplicadosHistoricos)
        {
            int ok = total - lowQuality - empty;
            var sb = new StringBuilder();
            sb.Append("<ul class=\"list-group\">");
            sb.Append($"<li class=\"list-group-item\"><strong>Carpeta:</strong> <code>{System.Web.HttpUtility.HtmlEncode(job)}</code></li>");
            sb.Append($"<li class=\"list-group-item\"><strong>Archivos procesados:</strong> {total}</li>");
            sb.Append($"<li class=\"list-group-item text-success\"><strong>Calidad OK:</strong> {ok}</li>");
            if (lowQuality > 0)
                sb.Append($"<li class=\"list-group-item text-warning\"><strong>Calidad baja (&lt;300 chars):</strong> {lowQuality}</li>");
            if (empty > 0)
                sb.Append($"<li class=\"list-group-item text-danger\"><strong>Vacíos (&lt;50 chars):</strong> {empty}</li>");
            if (duplicadosInternos > 0)
                sb.Append($"<li class=\"list-group-item text-secondary\"><strong>Duplicados internos del job:</strong> {duplicadosInternos}</li>");
            if (duplicadosHistoricos > 0)
                sb.Append($"<li class=\"list-group-item text-secondary\"><strong>Duplicados contra histórico:</strong> {duplicadosHistoricos}</li>");
            if (bomLimpiados > 0)
                sb.Append($"<li class=\"list-group-item\"><strong>BOM eliminados:</strong> {bomLimpiados}</li>");
            sb.Append("</ul>");
            sb.Append("<div class=\"mt-3\">");
            sb.Append("<p class=\"text-success\"><i class=\"fas fa-check-circle\"></i> <code>metadata.json</code> y sidecars <code>.metadata.json</code> actualizados correctamente.</p>");
            sb.Append("</div>");

            litResumen.Text = sb.ToString();
            phResumen.Visible = true;
        }
    }
}
