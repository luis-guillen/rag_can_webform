using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Web.Configuration;
using Newtonsoft.Json;

namespace rag_can_aspx.Services
{
    public class RagSource
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("domain")]
        public string Domain { get; set; }

        [JsonProperty("score")]
        public double? Score { get; set; }
    }

    public class RagQueryResponse
    {
        [JsonProperty("answer")]
        public string Answer { get; set; }

        [JsonProperty("sources")]
        public List<RagSource> Sources { get; set; } = new List<RagSource>();
    }

    public class RagQueryService
    {
        private static readonly HttpClient Http = new HttpClient();

        private readonly string _endpoint;

        public RagQueryService()
        {
            string configured = WebConfigurationManager.AppSettings["Rag:QueryEndpoint"];
            _endpoint = string.IsNullOrWhiteSpace(configured)
                ? "http://localhost:8000/query"
                : configured.Trim();

            int timeout = 60;
            if (int.TryParse(WebConfigurationManager.AppSettings["Rag:TimeoutSeconds"], out int t) && t > 0)
                timeout = t;
            Http.Timeout = TimeSpan.FromSeconds(timeout);
        }

        public RagQueryResponse Ask(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("La pregunta no puede estar vacía.");

            string payload = JsonConvert.SerializeObject(new { question = question.Trim() });

            using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
            {
                HttpResponseMessage resp = Http.PostAsync(_endpoint, content).GetAwaiter().GetResult();
                string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!resp.IsSuccessStatusCode)
                    throw new Exception($"La API respondió {(int)resp.StatusCode} ({resp.ReasonPhrase}). {Truncate(body, 300)}");

                RagQueryResponse result = JsonConvert.DeserializeObject<RagQueryResponse>(body);
                if (result == null)
                    throw new Exception("Respuesta vacía o no válida de la API.");
                if (result.Sources == null)
                    result.Sources = new List<RagSource>();
                return result;
            }
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
    }
}
