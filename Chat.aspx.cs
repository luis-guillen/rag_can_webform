using rag_can_aspx.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace rag_can_aspx
{
    public partial class Chat : Page
    {
        private const string ConversationIdKey = "Chat:ConversationId";

        private string ActiveConversationId
        {
            get { return Session[ConversationIdKey] as string; }
            set { Session[ConversationIdKey] = string.IsNullOrWhiteSpace(value) ? null : value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            lblError.Visible = false;

            if (!IsPostBack)
                LoadRequestedConversation();

            RenderHistorySidebar();
            RenderHealth();
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

                var history = new ChatHistoryService();
                ChatConversation conversation = LoadActiveConversation(history);
                if (conversation == null)
                    conversation = history.Create(pregunta);

                conversation.Turns.Add(new ChatHistoryTurn
                {
                    CreatedUtc = DateTime.UtcNow,
                    Question = pregunta,
                    Answer = resp.Answer,
                    AnswerMode = resp.AnswerMode,
                    Sources = resp.Sources ?? new List<RagSource>()
                });

                history.Save(conversation);
                ActiveConversationId = conversation.Id;

                txtPregunta.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo obtener respuesta: " + ex.Message);
            }

            RenderHistorySidebar();
            RenderConversacion();
        }

        protected void BtnLimpiar_Click(object sender, EventArgs e)
        {
            txtPregunta.Text = string.Empty;
            lblError.Visible = false;

            try
            {
                var history = new ChatHistoryService();
                ChatConversation conversation = LoadActiveConversation(history);
                if (conversation != null)
                {
                    conversation.Turns = new List<ChatHistoryTurn>();
                    conversation.Title = "Chat sin titulo";
                    history.Save(conversation);
                }
                else
                {
                    ActiveConversationId = null;
                }
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo limpiar la conversacion: " + ex.Message);
            }

            RenderHistorySidebar();
            RenderConversacion();
        }

        protected void BtnNuevoChat_Click(object sender, EventArgs e)
        {
            ActiveConversationId = null;
            txtPregunta.Text = string.Empty;
            lblError.Visible = false;
            RenderHistorySidebar();
            RenderConversacion();
        }

        protected void History_Command(object sender, CommandEventArgs e)
        {
            string id = e.CommandArgument == null ? null : e.CommandArgument.ToString();
            try
            {
                var history = new ChatHistoryService();
                if (e.CommandName == "OpenHistory")
                {
                    ChatConversation conversation = history.Load(id);
                    if (conversation == null)
                    {
                        MostrarError("La conversacion seleccionada ya no existe.");
                        ActiveConversationId = null;
                    }
                    else
                    {
                        ActiveConversationId = conversation.Id;
                    }
                }
                else if (e.CommandName == "DeleteHistory")
                {
                    history.Delete(id);
                    if (string.Equals(ActiveConversationId, id, StringComparison.OrdinalIgnoreCase))
                        ActiveConversationId = null;
                }
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo procesar el historial: " + ex.Message);
            }

            RenderHistorySidebar();
            RenderConversacion();
        }

        private void LoadRequestedConversation()
        {
            string requestedId = Request.QueryString["chat"];
            if (string.IsNullOrWhiteSpace(requestedId))
                return;

            try
            {
                ChatConversation conversation = new ChatHistoryService().Load(requestedId);
                if (conversation == null)
                    MostrarError("La conversacion solicitada no existe.");
                else
                    ActiveConversationId = conversation.Id;
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo abrir la conversacion solicitada: " + ex.Message);
            }
        }

        private ChatConversation LoadActiveConversation(ChatHistoryService history = null)
        {
            string id = ActiveConversationId;
            if (string.IsNullOrWhiteSpace(id))
                return null;

            history = history ?? new ChatHistoryService();
            return history.Load(id);
        }

        private void RenderConversacion()
        {
            ChatConversation conversation = null;
            try
            {
                conversation = LoadActiveConversation();
            }
            catch (Exception ex)
            {
                MostrarError("No se pudo cargar la conversacion activa: " + ex.Message);
                ActiveConversationId = null;
            }

            var turns = conversation == null || conversation.Turns == null
                ? new List<ChatHistoryTurn>()
                : conversation.Turns;

            if (turns.Count == 0)
            {
                litConversacion.Text =
                    "<div class=\"chat-empty\"><div class=\"chat-empty-icon\">💬</div>" +
                    "<div><strong>Aún no hay mensajes</strong></div>" +
                    "<div style=\"font-size: 13px; color: #555;\">Haz tu primera pregunta para comenzar</div></div>";
                return;
            }

            var sb = new StringBuilder();
            foreach (ChatHistoryTurn turn in turns)
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

                if (!string.IsNullOrWhiteSpace(turn.AnswerMode))
                    sb.Append("<div class=\"chat-answer-mode\">modo: ")
                      .Append(Enc(turn.AnswerMode))
                      .Append("</div>");

                if (turn.Sources != null && turn.Sources.Count > 0)
                    AppendSources(sb, turn.Sources);

                sb.Append("</div></div>");
            }

            litConversacion.Text = sb.ToString();
        }

        private void RenderHistorySidebar()
        {
            phHistory.Controls.Clear();

            IList<ChatHistorySummary> conversations;
            try
            {
                conversations = new ChatHistoryService().ListRecent();
            }
            catch (Exception ex)
            {
                phHistory.Controls.Add(new LiteralControl(
                    "<div class=\"history-empty\">No se pudo cargar el historial: " + Enc(ex.Message) + "</div>"));
                return;
            }

            if (conversations.Count == 0)
            {
                phHistory.Controls.Add(new LiteralControl(
                    "<div class=\"history-empty\">Aun no hay conversaciones guardadas</div>"));
                return;
            }

            phHistory.Controls.Add(new LiteralControl("<div class=\"history-list\">"));
            foreach (ChatHistorySummary conversation in conversations)
            {
                bool active = string.Equals(conversation.Id, ActiveConversationId, StringComparison.OrdinalIgnoreCase);
                phHistory.Controls.Add(new LiteralControl("<div class=\"history-item" + (active ? " active" : "") + "\">"));

                var open = new LinkButton
                {
                    ID = "open_" + conversation.Id,
                    Text = Enc(conversation.Title),
                    CssClass = "history-open",
                    CommandName = "OpenHistory",
                    CommandArgument = conversation.Id,
                    CausesValidation = false
                };
                open.Command += History_Command;
                phHistory.Controls.Add(open);

                phHistory.Controls.Add(new LiteralControl(
                    "<div class=\"history-meta\">" + Enc(FormatDate(conversation.UpdatedUtc)) +
                    " · " + conversation.TurnCount.ToString() + " turnos</div>"));

                var delete = new LinkButton
                {
                    ID = "delete_" + conversation.Id,
                    Text = "Eliminar",
                    CssClass = "history-delete",
                    CommandName = "DeleteHistory",
                    CommandArgument = conversation.Id,
                    CausesValidation = false,
                    OnClientClick = "return confirm('¿Eliminar esta conversación?');"
                };
                delete.Command += History_Command;
                phHistory.Controls.Add(delete);

                phHistory.Controls.Add(new LiteralControl("</div>"));
            }
            phHistory.Controls.Add(new LiteralControl("</div>"));
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

        private void RenderHealth()
        {
            try
            {
                var svc = new RagQueryService();
                RagHealth health = svc.GetHealth();
                var sb = new StringBuilder();
                sb.Append("<span class=\"health-pill health-ok\">API ")
                  .Append(Enc(string.IsNullOrWhiteSpace(health.Status) ? "ok" : health.Status))
                  .Append("</span>");
                sb.Append("<span class=\"health-pill\">coleccion ")
                  .Append(Enc(health.Collection ?? "-"))
                  .Append("</span>");
                sb.Append("<span class=\"health-pill\">puntos ")
                  .Append(health.QdrantPoints.HasValue ? health.QdrantPoints.Value.ToString() : "-")
                  .Append("</span>");
                sb.Append("<span class=\"health-pill\">modo ")
                  .Append(Enc(health.AnswerMode ?? "extractive"))
                  .Append("</span>");
                litHealth.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                litHealth.Text = "<span class=\"health-pill health-warn\">API sin health</span>" +
                                 "<span class=\"health-pill\">" + Enc(ex.Message) + "</span>";
            }
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

        private static string FormatDate(DateTime utc)
        {
            DateTime local = utc.Kind == DateTimeKind.Local ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
            return local.ToString("dd/MM/yyyy HH:mm");
        }
    }
}
