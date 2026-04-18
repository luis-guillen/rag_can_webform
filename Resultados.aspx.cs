using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;

namespace rag_can_aspx
{
    public partial class Resultados : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                var resultados = Session["Resultados"] as List<string>;
                var carpetaBase = Session["CarpetaBase"] as string;

                if (resultados == null)
                {
                    Response.Redirect("Default.aspx", false);
                    return;
                }

                Session["Resultados"] = null;
                Session["CarpetaBase"] = null;

                MostrarExito(resultados, carpetaBase);
            }
        }

        private void MostrarExito(List<string> resultados, string carpetaBase)
        {
            phSuccess.Visible = true;
            phError.Visible = false;

            litCarpeta.Text = HttpUtility.HtmlEncode(carpetaBase ?? "");

            var sb = new StringBuilder();
            foreach (var item in resultados)
            {
                sb.AppendLine(
                    $"<li style=\"padding: 10px; margin-bottom: 8px; background-color: #f5f5f5; " +
                    $"border-left: 4px solid #4caf50; border-radius: 2px;\">" +
                    $"{HttpUtility.HtmlEncode(item)}</li>");
            }
            litResultados.Text = sb.ToString();
        }

        private void MostrarError(string mensaje)
        {
            phError.Visible = true;
            phSuccess.Visible = false;
            litError.Text = $"<p>{HttpUtility.HtmlEncode(mensaje)}</p>";
        }
    }
}
