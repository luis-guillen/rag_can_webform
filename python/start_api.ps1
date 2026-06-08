# Levanta Qdrant (Docker) y la API FastAPI del RAG.
$ErrorActionPreference = "Stop"

# ── Qdrant (base de datos vectorial) ─────────────────────────────────────────
Write-Host "── [0] Qdrant ──────────────────────────────────────────────────────────" -ForegroundColor Cyan

if (-not (Test-Path ".\qdrant_storage")) {
    New-Item -ItemType Directory -Path ".\qdrant_storage" | Out-Null
}

$qdrantRunning = docker ps --format "{{.Names}}" 2>$null | Where-Object { $_ -eq "qdrant" }
$qdrantExists  = docker ps -a --format "{{.Names}}" 2>$null | Where-Object { $_ -eq "qdrant" }

if ($qdrantRunning) {
    Write-Host "Qdrant ya esta en ejecucion en http://localhost:6333" -ForegroundColor Green
} elseif ($qdrantExists) {
    Write-Host "Arrancando contenedor Qdrant existente..." -ForegroundColor Yellow
    docker start qdrant
    Write-Host "Qdrant listo en http://localhost:6333" -ForegroundColor Green
} else {
    Write-Host "Creando y arrancando contenedor Qdrant..." -ForegroundColor Yellow
    $StoragePath = (Resolve-Path ".\qdrant_storage").Path
    docker run -d `
        --name qdrant `
        --restart unless-stopped `
        -p 6333:6333 `
        -p 6334:6334 `
        -v "${StoragePath}:/qdrant/storage" `
        qdrant/qdrant
    Write-Host "Qdrant listo en http://localhost:6333" -ForegroundColor Green
}
Write-Host ""

# ── Python ────────────────────────────────────────────────────────────────────
$PythonCmd = "python"
$SiblingVenv = Join-Path $PSScriptRoot "..\..\rag_can_python\.venv"
if (Test-Path ".\.venv\Scripts\python.exe") {
    $PythonCmd = ".\.venv\Scripts\python.exe"
} elseif (Test-Path ".\.venv\bin\python") {
    $PythonCmd = ".\.venv\bin\python"
} elseif (Test-Path "$SiblingVenv\Scripts\python.exe") {
    $PythonCmd = "$SiblingVenv\Scripts\python.exe"
} elseif (Test-Path "$SiblingVenv\bin\python") {
    $PythonCmd = "$SiblingVenv\bin\python"
}

Write-Host "── [1] FastAPI ─────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "Usando Python : $PythonCmd" -ForegroundColor Cyan
Write-Host "Endpoint      : http://127.0.0.1:8000" -ForegroundColor Cyan
Write-Host "Health        : http://127.0.0.1:8000/health" -ForegroundColor Cyan
Write-Host "LLM local     : Ollama en http://127.0.0.1:11434, modelo qwen3.5:4b" -ForegroundColor Yellow
Write-Host "Pulsa Ctrl+C para detener FastAPI (Qdrant seguira corriendo en Docker)." -ForegroundColor Yellow
Write-Host ""

# ── Detectar LLM (remoto preferido) ──────────────────────────────────────────
$RemoteLlmUrl   = "http://10.17.159.197:11434"
$RemoteModel    = "qwen3:30b-a3b-instruct-2507-q4_K_M"
$LocalLlmUrl    = "http://127.0.0.1:11434"
$LocalModel     = "qwen3.5:4b"

Write-Host "   Detectando LLM disponible..." -ForegroundColor Cyan
try {
    $resp = Invoke-WebRequest -Uri "$RemoteLlmUrl/api/tags" -TimeoutSec 3 -UseBasicParsing -ErrorAction Stop
    Write-Host "   LLM remoto OK: $RemoteLlmUrl  →  $RemoteModel" -ForegroundColor Green
    $LlmUrl   = $RemoteLlmUrl
    $LlmModel = $RemoteModel
} catch {
    Write-Host "   LLM remoto no disponible, usando local: $LocalLlmUrl  →  $LocalModel" -ForegroundColor Yellow
    $LlmUrl   = $LocalLlmUrl
    $LlmModel = $LocalModel
}
Write-Host ""

if ([string]::IsNullOrWhiteSpace($env:RAG_LLM_ENABLED))          { $env:RAG_LLM_ENABLED = "true" }
if ([string]::IsNullOrWhiteSpace($env:RAG_LLM_BASE_URL))         { $env:RAG_LLM_BASE_URL = $LlmUrl }
if ([string]::IsNullOrWhiteSpace($env:RAG_LLM_API_KEY))          { $env:RAG_LLM_API_KEY = "ollama" }
if ([string]::IsNullOrWhiteSpace($env:RAG_LLM_MODEL))            { $env:RAG_LLM_MODEL = $LlmModel }
if ([string]::IsNullOrWhiteSpace($env:RAG_LLM_TIMEOUT_SECONDS))  { $env:RAG_LLM_TIMEOUT_SECONDS = "90" }
if ([string]::IsNullOrWhiteSpace($env:RAG_LLM_MAX_TOKENS))       { $env:RAG_LLM_MAX_TOKENS = "300" }
if ([string]::IsNullOrWhiteSpace($env:RAG_LLM_MAX_CONTEXT_CHARS)){ $env:RAG_LLM_MAX_CONTEXT_CHARS = "1800" }

& $PythonCmd -m uvicorn app.api:app --host 127.0.0.1 --port 8000
