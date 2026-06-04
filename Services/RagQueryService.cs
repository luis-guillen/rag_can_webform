using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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

    public class RagHealth
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("collection")]
        public string Collection { get; set; }

        [JsonProperty("qdrant_collection_exists")]
        public bool? QdrantCollectionExists { get; set; }

        [JsonProperty("qdrant_points")]
        public long? QdrantPoints { get; set; }

        [JsonProperty("answer_mode")]
        public string AnswerMode { get; set; }

        [JsonProperty("llm_enabled")]
        public bool? LlmEnabled { get; set; }
    }

    public class RagQueryResponse
    {
        [JsonProperty("answer")]
        public string Answer { get; set; }

        [JsonProperty("sources")]
        public List<RagSource> Sources { get; set; } = new List<RagSource>();

        [JsonProperty("answer_mode")]
        public string AnswerMode { get; set; }
    }

    public class RagQueryService
    {
        private readonly HttpClient _http;
        private readonly string _endpoint;
        private readonly string _healthEndpoint;
        private readonly int _topK;

        public RagQueryService()
        {
            string configured = WebConfigurationManager.AppSettings["Rag:QueryEndpoint"];
            _endpoint = string.IsNullOrWhiteSpace(configured)
                ? "http://localhost:8000/query"
                : configured.Trim();

            string healthConfigured = WebConfigurationManager.AppSettings["Rag:HealthEndpoint"];
            _healthEndpoint = string.IsNullOrWhiteSpace(healthConfigured)
                ? "http://localhost:8000/health"
                : healthConfigured.Trim();

            _topK = 5;
            if (int.TryParse(WebConfigurationManager.AppSettings["Rag:TopK"], out int k) && k > 0)
                _topK = k;

            int timeout = 60;
            if (int.TryParse(WebConfigurationManager.AppSettings["Rag:TimeoutSeconds"], out int t) && t > 0)
                timeout = t;

            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(timeout)
            };
        }

        public RagQueryResponse Ask(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("La pregunta no puede estar vacía.");

            string payload = JsonConvert.SerializeObject(new { question = question.Trim(), top_k = _topK });

            try
            {
                using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
                {
                    HttpResponseMessage resp = _http.PostAsync(_endpoint, content).GetAwaiter().GetResult();
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
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al enviar la solicitud a {_endpoint}: {FormatException(ex)}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception($"Timeout llamando a {_endpoint} tras {_http.Timeout.TotalSeconds:0} segundos: {FormatException(ex)}", ex);
            }
        }

        public RagHealth GetHealth()
        {
            try
            {
                HttpResponseMessage resp = _http.GetAsync(_healthEndpoint).GetAwaiter().GetResult();
                string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!resp.IsSuccessStatusCode)
                    throw new Exception($"La API respondió {(int)resp.StatusCode} ({resp.ReasonPhrase}). {Truncate(body, 300)}");

                RagHealth result = JsonConvert.DeserializeObject<RagHealth>(body);
                if (result == null)
                    throw new Exception("Respuesta health vacía o no válida de la API.");
                return result;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al consultar health en {_healthEndpoint}: {FormatException(ex)}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception($"Timeout consultando {_healthEndpoint} tras {_http.Timeout.TotalSeconds:0} segundos: {FormatException(ex)}", ex);
            }
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }

        private static string FormatException(Exception ex)
        {
            var sb = new StringBuilder(ex.Message);
            Exception inner = ex.InnerException;
            while (inner != null)
            {
                sb.Append(" | ").Append(inner.Message);
                inner = inner.InnerException;
            }
            return sb.ToString();
        }
    }
}
