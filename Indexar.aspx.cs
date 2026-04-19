using rag_can_aspx.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;

namespace rag_can_aspx
{
    public partial class Indexar : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                CargarDropdown();
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

            string[] archivos = Directory.GetFiles(carpetaAbs, "*.txt", opcion);

            int total = 0, lowQuality = 0, empty = 0, bomLimpiados = 0;
            var nuevasEntradas = new List<PageMetadata>();

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

                PageMetadata meta = svc.BuildForExistingPage(archivo, jobName);
                nuevasEntradas.Add(meta);

                if (meta.Quality == "empty") empty++;
                else if (meta.Quality == "low_quality") lowQuality++;
                total++;
            }

            // Resolver duplicados dentro del lote + contra entradas existentes
            var todas = svc.LoadAll();
            // Quitar entradas previas de este job para reemplazarlas
            todas.RemoveAll(e2 => string.Equals(e2.Job, jobName, StringComparison.OrdinalIgnoreCase));
            todas.AddRange(nuevasEntradas);
            svc.ResolveDuplicates(todas);
            svc.SaveAll(todas);

            int duplicados = nuevasEntradas.Count(m => m.DuplicateOf != null);

            MostrarResumen(jobName, total, lowQuality, empty, bomLimpiados, duplicados);
            CargarDropdown();
        }

        private void MostrarError(string mensaje)
        {
            lblError.Text = System.Web.HttpUtility.HtmlEncode(mensaje);
            lblError.Visible = true;
        }

        private void MostrarResumen(string job, int total, int lowQuality, int empty, int bomLimpiados, int duplicados)
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
            if (duplicados > 0)
                sb.Append($"<li class=\"list-group-item text-secondary\"><strong>Duplicados detectados:</strong> {duplicados}</li>");
            if (bomLimpiados > 0)
                sb.Append($"<li class=\"list-group-item\"><strong>BOM eliminados:</strong> {bomLimpiados}</li>");
            sb.Append("</ul>");
            sb.Append("<div class=\"mt-3\">");
            sb.Append("<p class=\"text-success\"><i class=\"fas fa-check-circle\"></i> <code>metadata.json</code> actualizado correctamente.</p>");
            sb.Append("</div>");

            litResumen.Text = sb.ToString();
            phResumen.Visible = true;
        }
    }
}
