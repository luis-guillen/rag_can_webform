# Script para ejecutar todo el flujo del RAG de manera secuencial en PowerShell.

$ErrorActionPreference = "Stop"

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

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Usando ejecutable de Python: $PythonCmd" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Corpus canonico: ..\App_Data\crawlings  (relativo a la raiz del .NET)" -ForegroundColor Cyan
Write-Host ""

# ── [0] Qdrant ────────────────────────────────────────────────────────────────
Write-Host "[0/5] Asegurando Qdrant (Docker)..." -ForegroundColor Yellow

if (-not (Test-Path ".\qdrant_storage")) {
    New-Item -ItemType Directory -Path ".\qdrant_storage" | Out-Null
}

$qdrantRunning = docker ps --format "{{.Names}}" 2>$null | Where-Object { $_ -eq "qdrant" }
$qdrantExists  = docker ps -a --format "{{.Names}}" 2>$null | Where-Object { $_ -eq "qdrant" }

if ($qdrantRunning) {
    Write-Host "   Qdrant ya en ejecucion en http://localhost:6333" -ForegroundColor Green
} elseif ($qdrantExists) {
    Write-Host "   Arrancando contenedor Qdrant existente..." -ForegroundColor Yellow
    docker start qdrant
    Write-Host "   Qdrant listo en http://localhost:6333" -ForegroundColor Green
} else {
    Write-Host "   Creando contenedor Qdrant..." -ForegroundColor Yellow
    $StoragePath = (Resolve-Path ".\qdrant_storage").Path
    docker run -d `
        --name qdrant `
        --restart unless-stopped `
        -p 6333:6333 `
        -p 6334:6334 `
        -v "${StoragePath}:/qdrant/storage" `
        qdrant/qdrant
    Write-Host "   Qdrant listo en http://localhost:6333" -ForegroundColor Green
}
Write-Host ""

Write-Host "[1/5] Validando el corpus..." -ForegroundColor Yellow
& $PythonCmd -m app.validate_corpus

Write-Host ""
Write-Host "[2/5] Generando chunks (solo paginas nuevas/cambiadas)..." -ForegroundColor Yellow
& $PythonCmd -m app.chunk --incremental

Write-Host ""
Write-Host "[3/5] Indexando en Qdrant (upsert incremental, sin recrear coleccion)..." -ForegroundColor Yellow
& $PythonCmd -m app.embed_index

Write-Host ""
Write-Host "[4/5] Ejecutando smoke tests de retrieval..." -ForegroundColor Yellow
& $PythonCmd scripts/smoke_test_retrieval.py

Write-Host ""
Write-Host "[5/5] Ejecutando preguntas de aceptacion..." -ForegroundColor Yellow
& $PythonCmd scripts/acceptance_questions.py

Write-Host ""
Write-Host "==================================================" -ForegroundColor Green
Write-Host "Proceso completado. Todo listo e indexado en Qdrant." -ForegroundColor Green
Write-Host "Levanta FastAPI con:" -ForegroundColor Green
Write-Host "  .\start_api.ps1" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
