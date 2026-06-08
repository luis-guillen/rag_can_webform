<%@ WebHandler Language="C#" Class="rag_can_aspx.DemoApiHandler" %>

using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Web;

namespace rag_can_aspx
{
    public class DemoApiHandler : IHttpHandler
    {
        public bool IsReusable => false;

        private static readonly object _startLock = new object();

        // ── Config ─────────────────────────────────────────────────────────────
        private static string Cfg(string key, string fallback = "")
            => ConfigurationManager.AppSettings[key] ?? fallback;

        private static string RemoteUrl   => Cfg("Llm:RemoteUrl",   "http://10.17.159.197:11434");
        private static string RemoteModel => Cfg("Llm:RemoteModel", "qwen3:30b-a3b-instruct-2507-q4_K_M");
        private static string LocalUrl    => Cfg("Llm:LocalUrl",    "http://127.0.0.1:11434");
        private static string LocalModel  => Cfg("Llm:LocalModel",  "qwen3.5:4b");

        // ── Entry ──────────────────────────────────────────────────────────────

        public void ProcessRequest(HttpContext ctx)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.Cache.SetNoStore();

            bool isPost = ctx.Request.HttpMethod == "POST";
            string action = isPost
                ? (ctx.Request.Form["action"] ?? "status")
                : (ctx.Request.QueryString["action"] ?? "status");

            switch (action)
            {
                case "start": WriteJson(ctx, isPost ? EnsureStarted(ctx) : GetStatus(ctx)); break;
                case "stop":  WriteJson(ctx, isPost ? StopApi(ctx)       : GetStatus(ctx)); break;
                default:      WriteJson(ctx, GetStatus(ctx)); break;
            }
        }

        // ── Response ───────────────────────────────────────────────────────────

        private struct R
        {
            public bool   Running;
            public string Msg;
            public string LlmUrl;
            public string LlmModel;
            public bool   LlmRemote;
        }

        private void WriteJson(HttpContext ctx, R r)
        {
            ctx.Response.Write(
                "{\"running\":"   + (r.Running   ? "true" : "false")
              + ",\"llmRemote\":" + (r.LlmRemote ? "true" : "false")
              + ",\"llmUrl\":\""    + Esc(r.LlmUrl)   + "\""
              + ",\"llmModel\":\"" + Esc(r.LlmModel) + "\""
              + ",\"message\":\""  + Esc(r.Msg)      + "\"}");
        }

        private string Esc(string s)
            => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"")
                        .Replace("\r", "").Replace("\n", " ");

        // ── Status ─────────────────────────────────────────────────────────────

        private R GetStatus(HttpContext ctx)
        {
            bool running    = IsApiRunning();
            string llmUrl   = ctx.Application["DemoApi:LlmUrl"]    as string ?? "";
            string llmModel = ctx.Application["DemoApi:LlmModel"]  as string ?? "";
            bool   llmRemote = ctx.Application["DemoApi:LlmRemote"] is bool b && b;

            if (running && string.IsNullOrEmpty(llmModel))
            {
                // API arrancada externamente — leer modelo real del /health y cachear
                var (url, model, remote) = ReadLlmFromHealth();
                llmUrl = url; llmModel = model; llmRemote = remote;
                ctx.Application["DemoApi:LlmUrl"]    = url;
                ctx.Application["DemoApi:LlmModel"]  = model;
                ctx.Application["DemoApi:LlmRemote"] = remote;
            }

            if (!running) { llmUrl = ""; llmModel = ""; llmRemote = false; }

            return new R
            {
                Running   = running,
                LlmUrl    = llmUrl,
                LlmModel  = llmModel,
                LlmRemote = llmRemote,
                Msg       = running ? "API en ejecución" : "API detenida"
            };
        }

        private (string url, string model, bool remote) ReadLlmFromHealth()
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:8000/health");
                req.Timeout = 1500;
                req.Method  = "GET";
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var sr   = new System.IO.StreamReader(resp.GetResponseStream()))
                {
                    string body = sr.ReadToEnd();
                    var m = System.Text.RegularExpressions.Regex.Match(body, "\"llm_model\"\\s*:\\s*\"([^\"]+)\"");
                    if (m.Success)
                    {
                        string model  = m.Groups[1].Value;
                        bool   remote = model == RemoteModel;
                        return (remote ? RemoteUrl : LocalUrl, model, remote);
                    }
                }
            }
            catch { }
            return DetectLlm();
        }

        private bool IsApiRunning()
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:8000/health");
                req.Timeout = 1500;
                req.Method  = "GET";
                using (var resp = (HttpWebResponse)req.GetResponse())
                    return (int)resp.StatusCode < 500;
            }
            catch { return false; }
        }

        // ── LLM detection ──────────────────────────────────────────────────────

        private (string url, string model, bool remote) DetectLlm()
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(RemoteUrl + "/api/tags");
                req.Timeout = 2500;
                req.Method  = "GET";
                using (var resp = (HttpWebResponse)req.GetResponse())
                    if ((int)resp.StatusCode < 400)
                        return (RemoteUrl, RemoteModel, true);
            }
            catch { }

            return (LocalUrl, LocalModel, false);
        }

        // ── Start ──────────────────────────────────────────────────────────────

        private R EnsureStarted(HttpContext ctx)
        {
            if (IsApiRunning())
                return GetStatus(ctx);

            lock (_startLock)
            {
                if (ctx.Application["DemoApi:Starting"] is bool b && b)
                    return new R { Running = false, Msg = "Inicio ya en progreso..." };

                if (IsApiRunning())
                    return GetStatus(ctx);

                var (llmUrl, llmModel, llmRemote) = DetectLlm();

                ctx.Application["DemoApi:Starting"] = true;
                ctx.Application["DemoApi:LlmUrl"]   = llmUrl;
                ctx.Application["DemoApi:LlmModel"] = llmModel;
                ctx.Application["DemoApi:LlmRemote"] = llmRemote;

                string pythonDir = ctx.Server.MapPath("~/python");
                string pythonExe = ResolvePythonExe(pythonDir);
                var app = ctx.Application;

                var t = new Thread(() =>
                {
                    try
                    {
                        EnsureQdrant(pythonDir);

                        var psi = new ProcessStartInfo(pythonExe,
                            "-m uvicorn app.api:app --host 127.0.0.1 --port 8000")
                        {
                            WorkingDirectory       = pythonDir,
                            UseShellExecute        = false,
                            RedirectStandardOutput = false,
                            RedirectStandardError  = false,
                            CreateNoWindow         = true
                        };
                        SetLlmEnv(psi, llmUrl, llmModel);

                        var proc = Process.Start(psi);
                        app.Lock();
                        app["DemoApi:Process"] = proc;
                        app.UnLock();
                        proc?.WaitForExit();
                    }
                    finally
                    {
                        app.Lock();
                        app.Remove("DemoApi:Starting");
                        app.Remove("DemoApi:Process");
                        app.Remove("DemoApi:LlmUrl");
                        app.Remove("DemoApi:LlmModel");
                        app.Remove("DemoApi:LlmRemote");
                        app.UnLock();
                    }
                });
                t.IsBackground = true;
                t.Start();

                string src = llmRemote ? "Dell Pro Max (remoto)" : "local";
                return new R
                {
                    Running   = false,
                    LlmUrl    = llmUrl,
                    LlmModel  = llmModel,
                    LlmRemote = llmRemote,
                    Msg       = "Iniciando con LLM " + src + "..."
                };
            }
        }

        // ── Stop ───────────────────────────────────────────────────────────────

        private R StopApi(HttpContext ctx)
        {
            var proc = ctx.Application["DemoApi:Process"] as Process;
            if (proc != null)
            {
                try { if (!proc.HasExited) proc.Kill(); } catch { }
                ctx.Application.Remove("DemoApi:Process");
                ctx.Application.Remove("DemoApi:Starting");
                ctx.Application.Remove("DemoApi:LlmUrl");
                ctx.Application.Remove("DemoApi:LlmModel");
                ctx.Application.Remove("DemoApi:LlmRemote");
            }
            return new R { Running = false, Msg = "API detenida" };
        }

        // ── Qdrant ─────────────────────────────────────────────────────────────

        private void EnsureQdrant(string pythonDir)
        {
            string running = RunDocker("ps --format {{.Names}}");
            if (running.Contains("qdrant")) return;

            string exists = RunDocker("ps -a --format {{.Names}}");
            if (exists.Contains("qdrant"))
            {
                RunDocker("start qdrant");
                return;
            }

            string storage = Path.Combine(pythonDir, "qdrant_storage");
            Directory.CreateDirectory(storage);
            RunDocker("run -d --name qdrant --restart unless-stopped"
                + " -p 6333:6333 -p 6334:6334"
                + " -v \"" + storage + ":/qdrant/storage\" qdrant/qdrant");
        }

        private string RunDocker(string args)
        {
            try
            {
                var psi = new ProcessStartInfo("docker", args)
                {
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true
                };
                var p = Process.Start(psi);
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return output ?? "";
            }
            catch { return ""; }
        }

        // ── Env / Python ────────────────────────────────────────────────────────

        private void SetLlmEnv(ProcessStartInfo psi, string llmUrl, string llmModel)
        {
            void Set(string k, string v)
            {
                if (!psi.EnvironmentVariables.ContainsKey(k))
                    psi.EnvironmentVariables[k] = v;
            }
            Set("RAG_LLM_ENABLED",           "true");
            Set("RAG_LLM_BASE_URL",          llmUrl);
            Set("RAG_LLM_API_KEY",           "ollama");
            Set("RAG_LLM_MODEL",             llmModel);
            Set("RAG_LLM_TIMEOUT_SECONDS",   "90");
            Set("RAG_LLM_MAX_TOKENS",        "300");
            Set("RAG_LLM_MAX_CONTEXT_CHARS", "1800");
        }

        private string ResolvePythonExe(string pythonDir)
        {
            string reposRoot = Path.GetFullPath(Path.Combine(pythonDir, "..", ".."));
            string[] candidates = {
                Path.Combine(pythonDir, ".venv", "Scripts", "python.exe"),
                Path.Combine(pythonDir, ".venv", "bin", "python"),
                Path.Combine(reposRoot, "rag_can_python", ".venv", "Scripts", "python.exe"),
                Path.Combine(reposRoot, "rag_can_python", ".venv", "bin", "python"),
            };
            foreach (string c in candidates)
            {
                try { string n = Path.GetFullPath(c); if (File.Exists(n)) return n; } catch { }
            }
            return "python";
        }
    }
}
