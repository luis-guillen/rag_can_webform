using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace rag_can_aspx.Services
{
    public class ChatHistoryTurn
    {
        public DateTime CreatedUtc { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public string AnswerMode { get; set; }
        public List<RagSource> Sources { get; set; } = new List<RagSource>();
    }

    public class ChatConversation
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public List<ChatHistoryTurn> Turns { get; set; } = new List<ChatHistoryTurn>();
    }

    public class ChatHistorySummary
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public int TurnCount { get; set; }
    }

    public class ChatHistoryService
    {
        private const int MaxTitleLength = 70;
        private readonly string _historyDirectory;

        public ChatHistoryService()
            : this(GetDefaultHistoryDirectory())
        {
        }

        public ChatHistoryService(string historyDirectory)
        {
            if (string.IsNullOrWhiteSpace(historyDirectory))
                throw new ArgumentException("La ruta del historial no puede estar vacia.", nameof(historyDirectory));

            _historyDirectory = historyDirectory;
        }

        public ChatConversation Create(string firstQuestion)
        {
            DateTime now = DateTime.UtcNow;
            return new ChatConversation
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = BuildTitle(firstQuestion),
                CreatedUtc = now,
                UpdatedUtc = now,
                Turns = new List<ChatHistoryTurn>()
            };
        }

        public ChatConversation Load(string id)
        {
            string path = GetPath(id);
            if (!File.Exists(path))
                return null;

            try
            {
                ChatConversation conversation = JsonConvert.DeserializeObject<ChatConversation>(File.ReadAllText(path));
                Normalize(conversation);
                if (conversation == null || !IsValidId(conversation.Id))
                    throw new InvalidOperationException("El archivo de historial no contiene una conversacion valida.");

                return conversation;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("La conversacion no se pudo leer porque el archivo JSON esta corrupto.", ex);
            }
        }

        public void Save(ChatConversation conversation)
        {
            if (conversation == null)
                throw new ArgumentNullException(nameof(conversation));

            Normalize(conversation);
            if (!IsValidId(conversation.Id))
                throw new InvalidOperationException("El identificador de conversacion no es valido.");

            EnsureDirectory();
            conversation.UpdatedUtc = DateTime.UtcNow;

            string json = JsonConvert.SerializeObject(conversation, Formatting.Indented);
            string path = GetPath(conversation.Id);
            string tempPath = path + ".tmp";

            File.WriteAllText(tempPath, json);
            if (File.Exists(path))
                File.Delete(path);
            File.Move(tempPath, path);
        }

        public IList<ChatHistorySummary> ListRecent(int limit = 30)
        {
            EnsureDirectory();

            var summaries = new List<ChatHistorySummary>();
            foreach (string path in Directory.GetFiles(_historyDirectory, "*.json"))
            {
                try
                {
                    ChatConversation conversation = JsonConvert.DeserializeObject<ChatConversation>(File.ReadAllText(path));
                    Normalize(conversation);
                    if (conversation == null || !IsValidId(conversation.Id))
                        continue;

                    summaries.Add(new ChatHistorySummary
                    {
                        Id = conversation.Id,
                        Title = conversation.Title,
                        CreatedUtc = conversation.CreatedUtc,
                        UpdatedUtc = conversation.UpdatedUtc,
                        TurnCount = conversation.Turns == null ? 0 : conversation.Turns.Count
                    });
                }
                catch
                {
                    // Broken history files should not break the chat page.
                }
            }

            return summaries
                .OrderByDescending(s => s.UpdatedUtc)
                .Take(Math.Max(1, limit))
                .ToList();
        }

        public bool Delete(string id)
        {
            string path = GetPath(id);
            if (!File.Exists(path))
                return false;

            File.Delete(path);
            return true;
        }

        private static string GetDefaultHistoryDirectory()
        {
            HttpContext context = HttpContext.Current;
            if (context == null)
                throw new InvalidOperationException("No hay HttpContext disponible para resolver App_Data.");

            return context.Server.MapPath("~/App_Data/chat-history/");
        }

        private string GetPath(string id)
        {
            if (!IsValidId(id))
                throw new ArgumentException("Identificador de conversacion no valido.", nameof(id));

            return Path.Combine(_historyDirectory, id + ".json");
        }

        private void EnsureDirectory()
        {
            Directory.CreateDirectory(_historyDirectory);
        }

        private static void Normalize(ChatConversation conversation)
        {
            if (conversation == null)
                return;

            if (conversation.Turns == null)
                conversation.Turns = new List<ChatHistoryTurn>();

            foreach (ChatHistoryTurn turn in conversation.Turns)
            {
                if (turn.Sources == null)
                    turn.Sources = new List<RagSource>();
            }

            if (conversation.CreatedUtc == default(DateTime))
                conversation.CreatedUtc = DateTime.UtcNow;
            if (conversation.UpdatedUtc == default(DateTime))
                conversation.UpdatedUtc = conversation.CreatedUtc;
            if (string.IsNullOrWhiteSpace(conversation.Title))
                conversation.Title = "Chat sin titulo";
        }

        private static bool IsValidId(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && Regex.IsMatch(id, "^[a-fA-F0-9]{32}$");
        }

        private static string BuildTitle(string question)
        {
            string title = Regex.Replace((question ?? string.Empty).Trim(), "\\s+", " ");
            if (string.IsNullOrWhiteSpace(title))
                return "Chat sin titulo";

            if (title.Length <= MaxTitleLength)
                return title;

            return title.Substring(0, MaxTitleLength).TrimEnd() + "...";
        }
    }
}
