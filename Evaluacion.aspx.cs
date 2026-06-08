using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.UI;

namespace rag_can_aspx
{
    public partial class Evaluacion : Page
    {
        private static readonly object _lock = new object();
        private const string AppKeyRunning = "Evaluacion:Running";

        private string ProjectRoot   => Server.MapPath("~");
        private string PythonDir     => Path.Combine(ProjectRoot, "python");
        private string ScriptPath    => Path.Combine(PythonDir, "scripts", "run_evaluation.py");
        private string ResultsPath   => Path.Combine(PythonDir, "evaluation", "results.json");
        private string RunLogPath    => Path.Combine(PythonDir, "evaluation", "run.log");

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                RefrescarResultados();
            SyncRunningState();
        }

        protected void BtnEvaluar_Click(object sender, EventArgs e)
        {
            lock (_lock)
            {
                if (Application[AppKeyRunning] is true)
                {
                    MostrarEstado("La evaluación ya está en curso. Espera a que termine.", false);
                    return;
                }
                Application[AppKeyRunning] = true;
            }

            tmrPoll.Enabled = true;
            MostrarEstado("Evaluación iniciada en segundo plano. Los resultados se mostrarán al finalizar.", true);

            string pythonExe  = ResolvePythonExe();
            string scriptPath = ScriptPath;
            string runLogPath = RunLogPath;
            string pythonDir  = PythonDir;
            HttpApplicationState app = Application;

            var thread = new Thread(() =>
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(runLogPath));

                    var psi = new ProcessStartInfo
                    {
                        FileName               = pythonExe,
                        Arguments              = "\"" + scriptPath + "\"",
                        WorkingDirectory       = pythonDir,
                        UseShellExecute        = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError  = true,
                        CreateNoWindow         = true,
                    };

                    using (var proc = Process.Start(psi))
                    using (var log  = new StreamWriter(runLogPath, false, Encoding.UTF8))
                    {
                        log.WriteLine("=== Evaluación iniciada: " + DateTime.UtcNow.ToString("o") + " ===");
                        log.WriteLine("Python : " + pythonExe);
                        log.WriteLine("Script : " + scriptPath);
                        log.Flush();

                        // Leer stdout y stderr antes de WaitForExit para evitar deadlock
                        string stdout = proc.StandardOutput.ReadToEnd();
                        string stderr = proc.StandardError.ReadToEnd();
                        proc.WaitForExit();

                        log.WriteLine("ExitCode: " + proc.ExitCode);
                        if (!string.IsNullOrWhiteSpace(stdout))
                            log.WriteLine(stdout);
                        if (!string.IsNullOrWhiteSpace(stderr))
                        {
                            log.WriteLine("--- STDERR ---");
                            log.WriteLine(stderr);
                        }
                        log.WriteLine("=== Completado: " + DateTime.UtcNow.ToString("o") + " ===");
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(runLogPath));
                        File.AppendAllText(runLogPath, "EXCEPTION: " + ex + Environment.NewLine);
                    }
                    catch { }
                }
                finally
                {
                    app[AppKeyRunning] = false;
                }
            });

            thread.IsBackground = true;
            thread.Start();
        }

        protected void TmrPoll_Tick(object sender, EventArgs e)
        {
            bool running = Application[AppKeyRunning] is true;
            if (!running)
            {
                tmrPoll.Enabled = false;
                MostrarEstado("Evaluación completada. Resultados actualizados.", true);
                phProgress.Controls.Clear();
            }
            else
            {
                phProgress.Controls.Clear();
                phProgress.Controls.Add(new LiteralControl(BuildProgressHtml()));
            }
            // Limpiar y volver a renderizar resultados
            phMetrics.Controls.Clear();
            phCategoryTable.Controls.Clear();
            phDifficultyTable.Controls.Clear();
            phFullResults.Controls.Clear();
            RefrescarResultados();
        }

        private void SyncRunningState()
        {
            bool running = Application[AppKeyRunning] is true;
            tmrPoll.Enabled = running;
            if (running && !IsPostBack)
            {
                MostrarEstado("Evaluación en curso en segundo plano...", true);
                phProgress.Controls.Add(new LiteralControl(BuildProgressHtml()));
            }
        }

        private void RefrescarResultados()
        {
            if (!File.Exists(ResultsPath))
            {
                phMetrics.Controls.Add(new LiteralControl(
                    "<div class=\"alert alert-secondary\">" +
                    "<i class=\"fas fa-info-circle me-2\"></i>" +
                    "Aún no hay resultados. Ejecuta la evaluación para generar métricas." +
                    "</div>"));
                return;
            }

            string json;
            try
            {
                json = File.ReadAllText(ResultsPath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                phMetrics.Controls.Add(new LiteralControl(
                    "<div class=\"alert alert-danger\">Error al leer results.json: " +
                    Enc(ex.Message) + "</div>"));
                return;
            }

            JObject data;
            try
            {
                data = JObject.Parse(json);
            }
            catch (Exception ex)
            {
                phMetrics.Controls.Add(new LiteralControl(
                    "<div class=\"alert alert-warning\">JSON pendiente de escritura o inválido: " +
                    Enc(ex.Message) + "</div>"));
                return;
            }

            JObject metrics   = data["metrics"] as JObject;
            JArray  results   = data["results"]  as JArray;
            string  genAt     = data["generated_at"]?.ToString() ?? "-";

            if (metrics == null)
            {
                phMetrics.Controls.Add(new LiteralControl(
                    "<div class=\"alert alert-warning\">El archivo de resultados no contiene métricas.</div>"));
                return;
            }

            phMetrics.Controls.Add(new LiteralControl(BuildMetricsHtml(metrics, genAt)));

            if (metrics["by_category"] is JObject byCat)
                phCategoryTable.Controls.Add(new LiteralControl(BuildCategoryTableHtml(byCat)));

            if (metrics["by_difficulty"] is JObject byDiff)
                phDifficultyTable.Controls.Add(new LiteralControl(BuildDifficultyTableHtml(byDiff)));

            if (results != null && results.Count > 0)
                phFullResults.Controls.Add(new LiteralControl(BuildFullResultsHtml(results)));
        }

        // ── HTML builders ────────────────────────────────────────────────────

        private static string BuildMetricsHtml(JObject m, string ts)
        {
            string r5  = FormatPct(m["recall_at_5"]);
            string mrr = FormatDec(m["mrr"], "0.0000");
            string lat = m["latency_avg_ms"] != null ? ((double)m["latency_avg_ms"]).ToString("0") + " ms" : "-";
            int total  = m["total"]?.ToObject<int>() ?? 0;

            var sb = new StringBuilder();
            sb.Append("<p class=\"text-muted small mb-3\"><i class=\"fas fa-clock me-1\"></i>Generado: ")
              .Append(Enc(ts))
              .Append("</p>");

            sb.Append("<div class=\"row row-cols-2 row-cols-md-4 g-3 mb-4\">");
            sb.Append(MetricCard("fas fa-list-ol",     "Total preguntas",  total.ToString(), "bg-secondary"));
            sb.Append(MetricCard("fas fa-bullseye",    "Recall@5",         r5,               "bg-primary"));
            sb.Append(MetricCard("fas fa-trophy",      "MRR",              mrr,              "bg-success"));
            sb.Append(MetricCard("fas fa-clock",       "Lat. media",       lat,              "bg-info text-dark"));
            sb.Append("</div>");

            // Fila secundaria
            sb.Append("<div class=\"row row-cols-2 row-cols-md-4 g-3 mb-4\">");
            sb.Append(MetricCard("fas fa-crosshairs",  "Recall@1",         FormatPct(m["recall_at_1"]), "bg-primary bg-opacity-75"));
            sb.Append(MetricCard("fas fa-crosshairs",  "Recall@3",         FormatPct(m["recall_at_3"]), "bg-primary bg-opacity-75"));
            sb.Append(MetricCard("fas fa-check-circle","Con fuentes",       FormatPct2(m["pct_with_sources"]),    "bg-success bg-opacity-75"));
            sb.Append(MetricCard("fas fa-ban",         "Rechazadas",       FormatPct2(m["pct_rejected"]),        "bg-danger bg-opacity-75"));
            sb.Append("</div>");

            return sb.ToString();
        }

        private static string MetricCard(string icon, string label, string value, string bg)
        {
            return "<div class=\"col\"><div class=\"card text-white " + bg + " h-100\">" +
                   "<div class=\"card-body text-center py-3\">" +
                   "<i class=\"" + icon + " fa-2x mb-2\"></i>" +
                   "<div class=\"fs-4 fw-bold\">" + Enc(value) + "</div>" +
                   "<div class=\"small\">" + Enc(label) + "</div>" +
                   "</div></div></div>";
        }

        private static string BuildCategoryTableHtml(JObject byCat)
        {
            string[] order  = { "retrieval", "synthesis", "multisource", "semantic", "reasoning" };
            string[] labels = { "Recuperación directa", "Síntesis", "Multifuente", "Semántica", "Razonamiento" };

            var sb = new StringBuilder();
            sb.Append("<h5 class=\"mt-2 mb-3\"><i class=\"fas fa-tags me-2\"></i>Rendimiento por tipo de pregunta</h5>");
            sb.Append("<div class=\"table-responsive mb-4\">");
            sb.Append("<table class=\"table table-bordered table-sm table-hover\">");
            sb.Append("<thead class=\"table-dark\"><tr>");
            sb.Append("<th>Tipo</th><th>N</th><th>Recall@5</th><th>MRR</th><th>Lat. media (ms)</th>");
            sb.Append("</tr></thead><tbody>");

            for (int i = 0; i < order.Length; i++)
            {
                var v = byCat[order[i]] as JObject;
                sb.Append("<tr>");
                sb.Append("<td><code>" + Enc(labels[i]) + "</code></td>");
                sb.Append("<td>" + (v?["n"]?.ToString() ?? "-") + "</td>");
                sb.Append("<td>" + FormatPct(v?["recall_at_5"]) + "</td>");
                sb.Append("<td>" + FormatDec(v?["mrr"], "0.0000") + "</td>");
                sb.Append("<td>" + (v?["latency_avg_ms"] != null ? ((double)v["latency_avg_ms"]).ToString("0") : "-") + "</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table></div>");
            return sb.ToString();
        }

        private static string BuildDifficultyTableHtml(JObject byDiff)
        {
            string[] order  = { "easy", "medium", "hard", "expert" };
            string[] labels = { "Fácil (Nivel 1)", "Medio (Nivel 2)", "Difícil (Niveles 3-4)", "Experto (Nivel 5)" };

            var sb = new StringBuilder();
            sb.Append("<h5 class=\"mt-2 mb-3\"><i class=\"fas fa-layer-group me-2\"></i>Rendimiento por dificultad</h5>");
            sb.Append("<div class=\"table-responsive mb-4\">");
            sb.Append("<table class=\"table table-bordered table-sm table-hover\">");
            sb.Append("<thead class=\"table-dark\"><tr>");
            sb.Append("<th>Nivel</th><th>N</th><th>Recall@5</th><th>MRR</th><th>Lat. media (ms)</th>");
            sb.Append("</tr></thead><tbody>");

            for (int i = 0; i < order.Length; i++)
            {
                var v = byDiff[order[i]] as JObject;
                sb.Append("<tr>");
                sb.Append("<td>" + Enc(labels[i]) + "</td>");
                sb.Append("<td>" + (v?["n"]?.ToString() ?? "-") + "</td>");
                sb.Append("<td>" + FormatPct(v?["recall_at_5"]) + "</td>");
                sb.Append("<td>" + FormatDec(v?["mrr"], "0.0000") + "</td>");
                sb.Append("<td>" + (v?["latency_avg_ms"] != null ? ((double)v["latency_avg_ms"]).ToString("0") : "-") + "</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table></div>");
            return sb.ToString();
        }

        private static string BuildFullResultsHtml(JArray results)
        {
            var sb = new StringBuilder();
            sb.Append("<h5 class=\"mt-2 mb-3\"><i class=\"fas fa-list-check me-2\"></i>Resultados por pregunta</h5>");
            sb.Append("<div class=\"accordion\" id=\"accordionEval\">");

            foreach (JObject r in results)
            {
                int    id       = r["id"]?.ToObject<int>() ?? 0;
                string q        = r["question"]?.ToString() ?? "";
                string diff     = r["difficulty"]?.ToString() ?? "";
                string cat      = r["category"]?.ToString() ?? "";
                bool   hasError = r["error"] != null;
                double? r5      = hasError ? (double?)null : r["recall_at_5"]?.ToObject<double?>();
                double? mrr     = hasError ? (double?)null : r["mrr"]?.ToObject<double?>();
                double  lat     = r["response_time_ms"]?.ToObject<double>() ?? 0;
                bool   rejected = !hasError && (r["rejected"]?.ToObject<bool>() ?? false);
                string mode     = r["answer_mode"]?.ToString() ?? "-";

                string badgeClass = hasError ? "bg-secondary" : (r5 == null ? "bg-secondary" : (r5 == 1.0 ? "bg-success" : "bg-danger"));
                string badgeText  = hasError ? "ERR" : (r5 == null ? "N/A" : (r5 == 1.0 ? "OK" : "MISS"));

                string qTrunc = q.Length > 90 ? q.Substring(0, 90) + "…" : q;

                sb.Append("<div class=\"accordion-item\">");
                sb.Append("<h2 class=\"accordion-header\" id=\"h" + id + "\">");
                sb.Append("<button class=\"accordion-button collapsed py-2\" type=\"button\" "
                        + "data-bs-toggle=\"collapse\" data-bs-target=\"#c" + id + "\" "
                        + "aria-expanded=\"false\" aria-controls=\"c" + id + "\">");
                sb.Append("<span class=\"badge " + badgeClass + " me-2\">" + badgeText + "</span>");
                sb.Append("<strong class=\"me-2\">Q" + id.ToString("D2") + "</strong>");
                sb.Append("<span class=\"badge bg-secondary me-1\">" + Enc(diff) + "</span>");
                sb.Append("<span class=\"badge bg-info text-dark me-2\">" + Enc(cat) + "</span>");
                sb.Append(Enc(qTrunc));
                sb.Append("</button></h2>");

                sb.Append("<div id=\"c" + id + "\" class=\"accordion-collapse collapse\" "
                        + "data-bs-parent=\"#accordionEval\">");
                sb.Append("<div class=\"accordion-body small\">");

                // Stats
                sb.Append("<p class=\"mb-1\">");
                sb.Append("<strong>MRR:</strong> " + (mrr.HasValue ? mrr.Value.ToString("0.0000") : "-"));
                sb.Append(" &nbsp;|&nbsp; <strong>Latencia:</strong> " + lat.ToString("0") + " ms");
                sb.Append(" &nbsp;|&nbsp; <strong>Modo:</strong> " + Enc(mode));
                if (rejected)
                    sb.Append(" &nbsp;<span class=\"badge bg-warning text-dark\">Rechazada</span>");
                sb.Append("</p>");

                // Fuentes
                if (!hasError && r["sources"] is JArray srcs && srcs.Count > 0)
                {
                    sb.Append("<p class=\"mb-1\"><strong>Fuentes recuperadas:</strong> ");
                    foreach (JObject src in srcs)
                    {
                        string dom = src["domain"]?.ToString() ?? "-";
                        double score = src["score"]?.ToObject<double>() ?? 0;
                        sb.Append("<span class=\"badge bg-primary me-1\">" + Enc(dom)
                                + " <small>" + score.ToString("0.00") + "</small></span>");
                    }
                    sb.Append("</p>");
                }

                // Preview respuesta
                if (!hasError)
                {
                    string ans = r["answer"]?.ToString() ?? "";
                    string preview = ans.Length > 320 ? ans.Substring(0, 320) + "…" : ans;
                    sb.Append("<div class=\"mt-2 text-muted border-start border-2 ps-2\">"
                            + Enc(preview) + "</div>");
                }
                else
                {
                    sb.Append("<div class=\"mt-2 text-danger\">Error: " + Enc(r["error"]?.ToString()) + "</div>");
                }

                sb.Append("</div></div></div>");
            }

            sb.Append("</div>");
            return sb.ToString();
        }

        // ── Utilidades ───────────────────────────────────────────────────────

        private void MostrarEstado(string msg, bool ok)
        {
            lblStatus.Text     = Enc(msg);
            lblStatus.CssClass = ok ? "alert alert-info d-block mb-3" : "alert alert-danger d-block mb-3";
            lblStatus.Visible  = true;
        }

        private string ResolvePythonExe()
        {
            // 1. Configuración explícita en Web.config (Eval:PythonExe)
            string configured = ConfigurationManager.AppSettings["Eval:PythonExe"];
            if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
                return configured;

            // 2. Búsqueda automática: venv relativo al proyecto y al repo hermano
            string reposRoot = Path.GetFullPath(Path.Combine(ProjectRoot, ".."));
            string[] candidates =
            {
                // Venv dentro de python/ de este mismo repo
                Path.Combine(PythonDir, ".venv", "Scripts", "python.exe"),
                Path.Combine(PythonDir, ".venv", "bin", "python"),
                // Repo hermano rag_can_python (patrón habitual de este proyecto)
                Path.Combine(reposRoot, "rag_can_python", ".venv", "Scripts", "python.exe"),
                Path.Combine(reposRoot, "rag_can_python", ".venv", "bin", "python"),
                // Venv en la raíz del repo webform
                Path.Combine(ProjectRoot, ".venv", "Scripts", "python.exe"),
                Path.Combine(ProjectRoot, ".venv", "bin", "python"),
            };
            foreach (string c in candidates)
            {
                try
                {
                    string norm = Path.GetFullPath(c);
                    if (File.Exists(norm))
                        return norm;
                }
                catch { }
            }
            return "python";
        }

        private string BuildProgressHtml()
        {
            var sb = new StringBuilder();
            sb.Append("<div class=\"card mb-4 border-info\">");
            sb.Append("<div class=\"card-body\">");
            sb.Append("<h6 class=\"card-title text-info\">");
            sb.Append("<span class=\"spinner-border spinner-border-sm me-2\" role=\"status\"></span>");
            sb.Append("Evaluación en curso — actualizando cada 3 s...</h6>");

            if (File.Exists(RunLogPath))
            {
                try
                {
                    // Leer las últimas líneas del log con FileShare.ReadWrite para no bloquear el proceso Python
                    string[] lines;
                    using (var fs = new FileStream(RunLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new System.IO.StreamReader(fs, Encoding.UTF8))
                    {
                        string content = sr.ReadToEnd();
                        lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    }

                    // Mostrar las últimas 15 líneas
                    int start = Math.Max(0, lines.Length - 15);
                    sb.Append("<pre style=\"max-height:220px;overflow:auto;background:#1e1e1e;color:#d4d4d4;padding:10px;border-radius:4px;font-size:0.82em;\">");
                    for (int i = start; i < lines.Length; i++)
                        sb.Append(Enc(lines[i]) + "\n");
                    sb.Append("</pre>");
                }
                catch { }
            }

            sb.Append("</div></div>");
            return sb.ToString();
        }

        private static string Enc(string s) =>
            HttpUtility.HtmlEncode(s ?? string.Empty);

        private static string FormatPct(JToken t)
        {
            if (t == null || t.Type == Newtonsoft.Json.Linq.JTokenType.Null)
                return "-";
            return (t.ToObject<double>() * 100).ToString("0.0") + "%";
        }

        private static string FormatPct2(JToken t)
        {
            if (t == null || t.Type == Newtonsoft.Json.Linq.JTokenType.Null)
                return "-";
            return t.ToObject<double>().ToString("0.0") + "%";
        }

        private static string FormatDec(JToken t, string fmt)
        {
            if (t == null || t.Type == Newtonsoft.Json.Linq.JTokenType.Null)
                return "-";
            return t.ToObject<double>().ToString(fmt);
        }
    }
}
