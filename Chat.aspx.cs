using rag_can_aspx.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;

namespace rag_can_aspx
{
    public partial class Chat : Page
    {
        [Serializable]
        private class ChatTurn
        {
            public string Question { get; set; }
            public string Answer { get; set; }
            public List<RagSource> Sources { get; set; } = new List<RagSource>();
        }

        private const string HistoryKey = "Chat:History";

        private List<ChatTurn> History
        {
            get
            {
                var h = Session[HistoryKey] as List<ChatTurn>;
                if (h == null)
                {
                    h = new List<ChatTurn>();
                    Session[HistoryKey] = h;
                }
                return h;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string endpoint = WebConfigurationManager.AppSettings["Rag:QueryEndpoint"];
            litEndpoint.Text = HttpUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(endpoint) ? "http://localhost:8000/query" : endpoint.Trim());

            if (!IsPostBack)
                RenderConversacion();
        }

        protected void BtnEnviar_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;
            string pregunta = txtPregunta.Text.Trim();

            if (string.IsNullOrWhiteSpace(pregunta))
            {
                MostrarError("Escribe una pregunta antes de enviar.");
                RenderConversacion();
                return;
            }

            try
            {
                var svc = new RagQueryService();
                RagQueryResponse resp = svc.Ask(pregunta);

                History.Add(new ChatTurn
                {
                    Question = pregunta,
                    Answer = resp.Answer,
                    Sources = resp.Sources ?? new List<RagSource>()
                });
                Session[HistoryKey] = History;

                txtPregunta.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo obtener respuesta: " + ex.Message);
            }

            RenderConversacion();
        }

        protected void BtnLimpiar_Click(object sender, EventArgs e)
        {
            Session[HistoryKey] = new List<ChatTurn>();
            txtPregunta.Text = string.Empty;
            lblError.Visible = false;
            RenderConversacion();
        }

        private void RenderConversacion()
        {
            var turns = History;
            if (turns.Count == 0)
            {
                litConversacion.Text =
                    "<div class=\"chat-empty\"><div class=\"chat-empty-icon\">💬</div>" +
                    "<div><strong>Aún no hay mensajes</strong></div>" +
                    "<div style=\"font-size: 13px; color: #555;\">Haz tu primera pregunta para comenzar</div></div>";
                return;
            }

            var sb = new StringBuilder();
            foreach (ChatTurn turn in turns)
            {
                // User message
                sb.Append("<div class=\"chat-message user\">");
                sb.Append("<div class=\"chat-message-avatar\">👤</div>");
                sb.Append("<div class=\"chat-message-content\">");
                sb.Append("<div class=\"chat-bubble\">");
                sb.Append(Enc(turn.Question));
                sb.Append("</div></div></div>");

                // Bot message
                sb.Append("<div class=\"chat-message bot\">");
                sb.Append("<div class=\"chat-message-avatar\">🤖</div>");
                sb.Append("<div class=\"chat-message-content\">");
                sb.Append("<div class=\"chat-bubble\">");
                sb.Append(string.IsNullOrWhiteSpace(turn.Answer)
                    ? "<em>(Sin respuesta)</em>"
                    : Enc(turn.Answer));
                sb.Append("</div>");

                if (turn.Sources != null && turn.Sources.Count > 0)
                    AppendSources(sb, turn.Sources);

                sb.Append("</div></div>");
            }

            litConversacion.Text = sb.ToString();
        }

        private void AppendSources(StringBuilder sb, List<RagSource> sources)
        {
            if (sources == null || sources.Count == 0)
                return;

            sb.Append("<div class=\"chat-sources\">");
            sb.Append("<div class=\"chat-sources-title\">📚 Fuentes</div>");
            sb.Append("<ul class=\"chat-sources-list\">");

            foreach (RagSource src in sources)
            {
                sb.Append("<li>");

                string title = string.IsNullOrWhiteSpace(src.Title)
                    ? (string.IsNullOrWhiteSpace(src.Url) ? "(sin título)" : src.Url)
                    : src.Title;

                if (!string.IsNullOrWhiteSpace(src.Url))
                {
                    sb.Append("<a href=\"").Append(Enc(src.Url))
                      .Append("\" target=\"_blank\" rel=\"noopener noreferrer\">")
                      .Append(Enc(title)).Append("</a>");
                }
                else
                {
                    sb.Append(Enc(title));
                }

                if (!string.IsNullOrWhiteSpace(src.Domain))
                    sb.Append("<span class=\"chat-sources-badge\">").Append(Enc(src.Domain)).Append("</span>");

                if (src.Score.HasValue)
                    sb.Append("<span class=\"chat-sources-badge\" style=\"margin-left: 0.25rem;\">")
                      .Append(src.Score.Value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
                      .Append("</span>");

                sb.Append("</li>");
            }

            sb.Append("</ul></div>");
        }

        private void MostrarError(string mensaje)
        {
            lblError.Text = HttpUtility.HtmlEncode(mensaje);
            lblError.Visible = true;
        }

        private static string Enc(string s)
        {
            return HttpUtility.HtmlEncode(s ?? string.Empty);
        }
    }
}
