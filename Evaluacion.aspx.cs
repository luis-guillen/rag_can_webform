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

        private string ProjectRoot => Server.MapPath("~");
        private string PythonDir   => Path.Combine(ProjectRoot, "python");
        private string ScriptPath  => Path.Combine(PythonDir, "scripts", "run_evaluation.py");

        private string ResultsPathFor(string label) => Path.Combine(PythonDir, "evaluation", $"results_{label}.json");
        private string RunLogPathFor(string label)  => Path.Combine(PythonDir, "evaluation", $"run_{label}.log");
        private string AppKey(string label)         => "Evaluacion:Running:" + label;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                RefrescarResultados(activeLabel: null);
            SyncRunningState();
        }

        protected void BtnEvaluarRemoto_Click(object sender, EventArgs e) => LaunchEvaluation("remote");
        protected void BtnEvaluarLocal_Click(object sender, EventArgs e)  => LaunchEvaluation("local");

        private void LaunchEvaluation(string label)
        {
            lock (_lock)
            {
                if (Application[AppKey(label)] is true)
                {
                    MostrarEstado($"La evaluación '{label}' ya está en curso.", false);
                    return;
                }
                Application[AppKey(label)] = true;
            }

            tmrPoll.Enabled = true;
            MostrarEstado($"Evaluación '{label}' iniciada en segundo plano (~3 min).", true);

            string pythonExe  = ResolvePythonExe();
            string scriptPath = ScriptPath;
            string logPath    = RunLogPathFor(label);
            string pythonDir  = PythonDir;
            string lbl        = label;
            HttpApplicationState app = Application;

            var thread = new Thread(() =>
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logPath));

                    var psi = new ProcessStartInfo
                    {
                        FileName               = pythonExe,
                        Arguments              = $"\"{scriptPath}\" --label {lbl}",
                        WorkingDirectory       = pythonDir,
                        UseShellExecute        = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError  = true,
                        CreateNoWindow         = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding  = Encoding.UTF8,
                    };
                    psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
                    psi.EnvironmentVariables["PYTHONUTF8"]       = "1";

                    using (var proc = Process.Start(psi))
                    using (var log  = new StreamWriter(logPath, false, Encoding.UTF8))
                    {
                        log.WriteLine($"=== Evaluación [{lbl}] iniciada: {DateTime.UtcNow:o} ===");
                        log.WriteLine("Python : " + pythonExe);
                        log.WriteLine("Script : " + scriptPath);
                        log.Flush();

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
                        log.WriteLine($"=== Completado: {DateTime.UtcNow:o} ===");
                    }
                }
                catch (Exception ex)
                {
                    try { File.AppendAllText(logPath, "EXCEPTION: " + ex + Environment.NewLine); } catch { }
                }
                finally
                {
                    app[AppKey(lbl)] = false;
                }
            });

            thread.IsBackground = true;
            thread.Start();
        }

        protected void TmrPoll_Tick(object sender, EventArgs e)
        {
            bool runningRemote = Application[AppKey("remote")] is true;
            bool runningLocal  = Application[AppKey("local")]  is true;

            if (!runningRemote && !runningLocal)
            {
                tmrPoll.Enabled = false;
                MostrarEstado("Evaluación completada. Resultados actualizados.", true);
                phProgress.Controls.Clear();
            }
            else
            {
                phProgress.Controls.Clear();
                string activeLog = runningRemote ? RunLogPathFor("remote") : RunLogPathFor("local");
                string runLabel  = runningRemote ? "remote" : "local";
                phProgress.Controls.Add(new LiteralControl(BuildProgressHtml(activeLog, runLabel)));
            }

            phTabs.Controls.Clear();
            phMetrics.Controls.Clear();
            phCategoryTable.Controls.Clear();
            phDifficultyTable.Controls.Clear();
            phFullResults.Controls.Clear();
            RefrescarResultados(activeLabel: runningRemote ? "remote" : (runningLocal ? "local" : null));
        }

        private void SyncRunningState()
        {
            bool runningRemote = Application[AppKey("remote")] is true;
            bool runningLocal  = Application[AppKey("local")]  is true;
            bool anyRunning    = runningRemote || runningLocal;

            tmrPoll.Enabled = anyRunning;
            if (anyRunning && !IsPostBack)
            {
                string lbl = runningRemote ? "remote" : "local";
                MostrarEstado($"Evaluación '{lbl}' en curso en segundo plano...", true);
                phProgress.Controls.Add(new LiteralControl(BuildProgressHtml(RunLogPathFor(lbl), lbl)));
            }
        }

        // ── Tabs + resultados ────────────────────────────────────────────────

        private void RefrescarResultados(string activeLabel)
        {
            bool hasRemote = File.Exists(ResultsPathFor("remote"));
            bool hasLocal  = File.Exists(ResultsPathFor("local"));

            if (!hasRemote && !hasLocal)
            {
                phMetrics.Controls.Add(new LiteralControl(
                    "<div class=\"alert alert-secondary\">" +
                    "<i class=\"fas fa-info-circle me-2\"></i>" +
                    "Aún no hay resultados. Ejecuta la evaluación con uno de los botones." +
                    "</div>"));
                return;
            }

            // Determinar tab activo: preferir el que acaba de correr, luego remote, luego local
            string defaultTab = activeLabel ?? (hasRemote ? "remote" : "local");

            if (hasRemote && hasLocal)
            {
                // Tabs Bootstrap
                phTabs.Controls.Add(new LiteralControl(BuildTabNavHtml(defaultTab)));

                var sbContent = new StringBuilder();
                sbContent.Append("<div class=\"tab-content mt-3\">");
                sbContent.Append(BuildTabPane("remote", defaultTab == "remote", "Remoto — Dell Pro Max"));
                sbContent.Append(BuildTabPane("local",  defaultTab == "local",  "Local — Ollama"));
                sbContent.Append("</div>");
                phMetrics.Controls.Add(new LiteralControl(sbContent.ToString()));
            }
            else
            {
                // Solo un resultado: sin tabs
                string lbl = hasRemote ? "remote" : "local";
                RenderLabelResults(lbl, phMetrics, phCategoryTable, phDifficultyTable, phFullResults);
            }
        }

        private string BuildTabNavHtml(string active)
        {
            string remActive = active == "remote" ? " active" : "";
            string locActive = active == "local"  ? " active" : "";
            return
                "<ul class=\"nav nav-tabs\" id=\"evalTabs\" role=\"tablist\">" +
                $"<li class=\"nav-item\" role=\"presentation\">" +
                $"<button class=\"nav-link{remActive}\" id=\"tab-remote\" data-bs-toggle=\"tab\" " +
                $"data-bs-target=\"#pane-remote\" type=\"button\" role=\"tab\">" +
                "<i class=\"fas fa-server me-1\"></i>LLM remoto (Dell Pro Max)</button></li>" +
                $"<li class=\"nav-item\" role=\"presentation\">" +
                $"<button class=\"nav-link{locActive}\" id=\"tab-local\" data-bs-toggle=\"tab\" " +
                $"data-bs-target=\"#pane-local\" type=\"button\" role=\"tab\">" +
                "<i class=\"fas fa-laptop me-1\"></i>LLM local (Ollama)</button></li>" +
                "</ul>";
        }

        private string BuildTabPane(string label, bool active, string title)
        {
            string activeClass = active ? " show active" : "";
            var sb = new StringBuilder();
            sb.Append($"<div class=\"tab-pane fade{activeClass}\" id=\"pane-{label}\" role=\"tabpanel\">");
            sb.Append($"<div class=\"mt-3\">");

            string path = ResultsPathFor(label);
            if (!File.Exists(path))
            {
                sb.Append("<div class=\"alert alert-secondary\">Sin resultados para este modo.</div>");
                sb.Append("</div></div>");
                return sb.ToString();
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                JObject data    = JObject.Parse(json);
                JObject metrics = data["metrics"] as JObject;
                JArray  results = data["results"]  as JArray;
                string  genAt   = data["generated_at"]?.ToString() ?? "-";

                if (metrics != null)
                {
                    sb.Append(BuildMetricsHtml(metrics, genAt));
                    if (metrics["by_category"]   is JObject byCat)  sb.Append(BuildCategoryTableHtml(byCat));
                    if (metrics["by_difficulty"]  is JObject byDiff) sb.Append(BuildDifficultyTableHtml(byDiff));
                    if (results != null && results.Count > 0)        sb.Append(BuildFullResultsHtml(results, label));
                }
            }
            catch (Exception ex)
            {
                sb.Append("<div class=\"alert alert-danger\">Error al leer resultados: " + Enc(ex.Message) + "</div>");
            }

            sb.Append("</div></div>");
            return sb.ToString();
        }

        private void RenderLabelResults(string label,
            System.Web.UI.WebControls.PlaceHolder phM,
            System.Web.UI.WebControls.PlaceHolder phC,
            System.Web.UI.WebControls.PlaceHolder phD,
            System.Web.UI.WebControls.PlaceHolder phF)
        {
            string path = ResultsPathFor(label);
            string json;
            try { json = File.ReadAllText(path, Encoding.UTF8); }
            catch (Exception ex) { phM.Controls.Add(new LiteralControl("<div class=\"alert alert-danger\">Error: " + Enc(ex.Message) + "</div>")); return; }

            JObject data;
            try { data = JObject.Parse(json); }
            catch (Exception ex) { phM.Controls.Add(new LiteralControl("<div class=\"alert alert-warning\">JSON inválido: " + Enc(ex.Message) + "</div>")); return; }

            JObject metrics = data["metrics"] as JObject;
            JArray  results = data["results"]  as JArray;
            string  genAt   = data["generated_at"]?.ToString() ?? "-";

            if (metrics == null) { phM.Controls.Add(new LiteralControl("<div class=\"alert alert-warning\">Sin métricas.</div>")); return; }

            phM.Controls.Add(new LiteralControl(BuildMetricsHtml(metrics, genAt)));
            if (metrics["by_category"]  is JObject byCat)  phC.Controls.Add(new LiteralControl(BuildCategoryTableHtml(byCat)));
            if (metrics["by_difficulty"] is JObject byDiff) phD.Controls.Add(new LiteralControl(BuildDifficultyTableHtml(byDiff)));
            if (results != null && results.Count > 0)       phF.Controls.Add(new LiteralControl(BuildFullResultsHtml(results, label)));
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
              .Append(Enc(ts)).Append("</p>");

            sb.Append("<div class=\"row row-cols-2 row-cols-md-4 g-3 mb-4\">");
            sb.Append(MetricCard("fas fa-list-ol",  "Total preguntas", total.ToString(), "bg-secondary"));
            sb.Append(MetricCard("fas fa-bullseye", "Recall@5",        r5,              "bg-primary"));
            sb.Append(MetricCard("fas fa-trophy",   "MRR",             mrr,             "bg-success"));
            sb.Append(MetricCard("fas fa-clock",    "Lat. media",      lat,             "bg-info text-dark"));
            sb.Append("</div>");

            sb.Append("<div class=\"row row-cols-2 row-cols-md-4 g-3 mb-4\">");
            sb.Append(MetricCard("fas fa-crosshairs",  "Recall@1",    FormatPct(m["recall_at_1"]),          "bg-primary bg-opacity-75"));
            sb.Append(MetricCard("fas fa-crosshairs",  "Recall@3",    FormatPct(m["recall_at_3"]),          "bg-primary bg-opacity-75"));
            sb.Append(MetricCard("fas fa-check-circle","Con fuentes",  FormatPct2(m["pct_with_sources"]),   "bg-success bg-opacity-75"));
            sb.Append(MetricCard("fas fa-ban",         "Rechazadas",   FormatPct2(m["pct_rejected"]),       "bg-danger bg-opacity-75"));
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
            sb.Append("<div class=\"table-responsive mb-4\"><table class=\"table table-bordered table-sm table-hover\">");
            sb.Append("<thead class=\"table-dark\"><tr><th>Tipo</th><th>N</th><th>Recall@5</th><th>MRR</th><th>Lat. media (ms)</th></tr></thead><tbody>");

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
            sb.Append("<div class=\"table-responsive mb-4\"><table class=\"table table-bordered table-sm table-hover\">");
            sb.Append("<thead class=\"table-dark\"><tr><th>Nivel</th><th>N</th><th>Recall@5</th><th>MRR</th><th>Lat. media (ms)</th></tr></thead><tbody>");

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

        private static string BuildFullResultsHtml(JArray results, string labelPrefix)
        {
            var sb = new StringBuilder();
            sb.Append("<h5 class=\"mt-2 mb-3\"><i class=\"fas fa-list-check me-2\"></i>Resultados por pregunta</h5>");
            sb.Append($"<div class=\"accordion\" id=\"accordion_{labelPrefix}\">");

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
                bool    rej     = !hasError && (r["rejected"]?.ToObject<bool>() ?? false);
                string  mode    = r["answer_mode"]?.ToString() ?? "-";

                string bClass = hasError ? "bg-secondary" : (r5 == null ? "bg-secondary" : (r5 == 1.0 ? "bg-success" : "bg-danger"));
                string bText  = hasError ? "ERR" : (r5 == null ? "N/A" : (r5 == 1.0 ? "OK" : "MISS"));
                string qT     = q.Length > 90 ? q.Substring(0, 90) + "…" : q;
                string uid    = labelPrefix + "_" + id;

                sb.Append("<div class=\"accordion-item\">");
                sb.Append($"<h2 class=\"accordion-header\" id=\"h{uid}\">");
                sb.Append($"<button class=\"accordion-button collapsed py-2\" type=\"button\" " +
                          $"data-bs-toggle=\"collapse\" data-bs-target=\"#c{uid}\" " +
                          $"aria-expanded=\"false\" aria-controls=\"c{uid}\">");
                sb.Append($"<span class=\"badge {bClass} me-2\">{bText}</span>");
                sb.Append($"<strong class=\"me-2\">Q{id:D2}</strong>");
                sb.Append($"<span class=\"badge bg-secondary me-1\">{Enc(diff)}</span>");
                sb.Append($"<span class=\"badge bg-info text-dark me-2\">{Enc(cat)}</span>");
                sb.Append(Enc(qT));
                sb.Append("</button></h2>");

                sb.Append($"<div id=\"c{uid}\" class=\"accordion-collapse collapse\" " +
                          $"data-bs-parent=\"#accordion_{labelPrefix}\">");
                sb.Append("<div class=\"accordion-body small\">");

                sb.Append("<p class=\"mb-1\">");
                sb.Append("<strong>MRR:</strong> " + (mrr.HasValue ? mrr.Value.ToString("0.0000") : "-"));
                sb.Append(" &nbsp;|&nbsp; <strong>Latencia:</strong> " + lat.ToString("0") + " ms");
                sb.Append(" &nbsp;|&nbsp; <strong>Modo:</strong> " + Enc(mode));
                if (rej) sb.Append(" &nbsp;<span class=\"badge bg-warning text-dark\">Rechazada</span>");
                sb.Append("</p>");

                if (!hasError && r["sources"] is JArray srcs && srcs.Count > 0)
                {
                    sb.Append("<p class=\"mb-1\"><strong>Fuentes:</strong> ");
                    foreach (JObject src in srcs)
                    {
                        string dom   = src["domain"]?.ToString() ?? "-";
                        double score = src["score"]?.ToObject<double>() ?? 0;
                        sb.Append($"<span class=\"badge bg-primary me-1\">{Enc(dom)} <small>{score:0.00}</small></span>");
                    }
                    sb.Append("</p>");
                }

                if (!hasError)
                {
                    string ans = r["answer"]?.ToString() ?? "";
                    string pre = ans.Length > 320 ? ans.Substring(0, 320) + "…" : ans;
                    sb.Append("<div class=\"mt-2 text-muted border-start border-2 ps-2\">" + Enc(pre) + "</div>");
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

        private string BuildProgressHtml(string logPath, string label)
        {
            var sb = new StringBuilder();
            sb.Append("<div class=\"card mb-4 border-info\"><div class=\"card-body\">");
            sb.Append("<h6 class=\"card-title text-info\">");
            sb.Append("<span class=\"spinner-border spinner-border-sm me-2\" role=\"status\"></span>");
            sb.Append($"Evaluando [{label}] — actualizando cada 3 s...</h6>");

            if (File.Exists(logPath))
            {
                try
                {
                    string[] lines;
                    using (var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, Encoding.UTF8))
                    {
                        string content = sr.ReadToEnd();
                        lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    int start = Math.Max(0, lines.Length - 15);
                    sb.Append("<pre class=\"app-log app-log-sm\" style=\"max-height:220px;\">");
                    for (int i = start; i < lines.Length; i++)
                        sb.Append(Enc(lines[i]) + "\n");
                    sb.Append("</pre>");
                }
                catch { }
            }

            sb.Append("</div></div>");
            return sb.ToString();
        }

        private string ResolvePythonExe()
        {
            string configured = ConfigurationManager.AppSettings["Eval:PythonExe"];
            if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
                return configured;

            string reposRoot = Path.GetFullPath(Path.Combine(ProjectRoot, ".."));
            string[] candidates =
            {
                Path.Combine(PythonDir, ".venv", "Scripts", "python.exe"),
                Path.Combine(PythonDir, ".venv", "bin", "python"),
                Path.Combine(reposRoot, "rag_can_python", ".venv", "Scripts", "python.exe"),
                Path.Combine(reposRoot, "rag_can_python", ".venv", "bin", "python"),
                Path.Combine(ProjectRoot, ".venv", "Scripts", "python.exe"),
                Path.Combine(ProjectRoot, ".venv", "bin", "python"),
            };
            foreach (string c in candidates)
            {
                try { if (File.Exists(Path.GetFullPath(c))) return Path.GetFullPath(c); } catch { }
            }
            return "python";
        }

        private static string Enc(string s)    => HttpUtility.HtmlEncode(s ?? string.Empty);
        private static string FormatPct(JToken t)  => (t == null || t.Type == Newtonsoft.Json.Linq.JTokenType.Null) ? "-" : (t.ToObject<double>() * 100).ToString("0.0") + "%";
        private static string FormatPct2(JToken t) => (t == null || t.Type == Newtonsoft.Json.Linq.JTokenType.Null) ? "-" : t.ToObject<double>().ToString("0.0") + "%";
        private static string FormatDec(JToken t, string fmt) => (t == null || t.Type == Newtonsoft.Json.Linq.JTokenType.Null) ? "-" : t.ToObject<double>().ToString(fmt);
    }
}
