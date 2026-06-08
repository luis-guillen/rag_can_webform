# Levanta la API FastAPI del RAG en el puerto que consume Chat.aspx.
$ErrorActionPreference = "Stop"

$PythonCmd = "python"
if (Test-Path ".\.venv\Scripts\python.exe") {
    $PythonCmd = ".\.venv\Scripts\python.exe"
} elseif (Test-Path ".\.venv\bin\python") {
    $PythonCmd = ".\.venv\bin\python"
}

Write-Host "Usando Python: $PythonCmd" -ForegroundColor Cyan
Write-Host "Endpoint: http://127.0.0.1:8000" -ForegroundColor Cyan
Write-Host "Health:   http://127.0.0.1:8000/health" -ForegroundColor Cyan
Write-Host ""
Write-Host "Si Qdrant no esta levantado en http://localhost:6333, /health saldra degraded." -ForegroundColor Yellow
Write-Host "LLM local: Ollama en http://127.0.0.1:11434, modelo qwen3.5:4b." -ForegroundColor Yellow
Write-Host "Pulsa Ctrl+C para detener FastAPI." -ForegroundColor Yellow
Write-Host ""

if ([string]::IsNullOrWhiteSpace($env:RAG_LLM_ENABLED)) {
    $env:RAG_LLM_ENABLED = "true"
}
if ([string]::IsNullOrWhiteSpace($env:RAG_LLM_BASE_URL)) {
    $env:RAG_LLM_BASE_URL = "http://127.0.0.1:11434"
}
if ([string]::IsNullOrWhiteSpace($env:RAG_LLM_API_KEY)) {
    $env:RAG_LLM_API_KEY = "ollama"
}
if ([string]::IsNullOrWhiteSpace($env:RAG_LLM_MODEL)) {
    $env:RAG_LLM_MODEL = "qwen3.5:4b"
}
if ([string]::IsNullOrWhiteSpace($env:RAG_LLM_TIMEOUT_SECONDS)) {
    $env:RAG_LLM_TIMEOUT_SECONDS = "90"
}
if ([string]::IsNullOrWhiteSpace($env:RAG_LLM_MAX_TOKENS)) {
    $env:RAG_LLM_MAX_TOKENS = "300"
}
if ([string]::IsNullOrWhiteSpace($env:RAG_LLM_MAX_CONTEXT_CHARS)) {
    $env:RAG_LLM_MAX_CONTEXT_CHARS = "1800"
}

& $PythonCmd -m uvicorn app.api:app --host 127.0.0.1 --port 8000
