# Script para ejecutar todo el flujo del RAG de manera secuencial en PowerShell.

# Detener la ejecución si hay un error
$ErrorActionPreference = "Stop"

# Detectar el ejecutable de Python correcto en el venv
$PythonCmd = "python"
if (Test-Path ".\.venv\Scripts\python.exe") {
    $PythonCmd = ".\.venv\Scripts\python.exe"
} elseif (Test-Path ".\.venv\bin\python") {
    $PythonCmd = ".\.venv\bin\python"
}

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Usando ejecutable de Python: $PythonCmd" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Corpus canonico: ..\App_Data\crawlings  (relativo a la raiz del .NET)" -ForegroundColor Cyan
Write-Host ""

Write-Host "🚀 [1/5] Validando el corpus..." -ForegroundColor Yellow
& $PythonCmd -m app.validate_corpus

Write-Host ""
Write-Host "🚀 [2/5] Generando chunks (solo paginas nuevas/cambiadas)..." -ForegroundColor Yellow
& $PythonCmd -m app.chunk --incremental

Write-Host ""
Write-Host "🚀 [3/5] Indexando en Qdrant (upsert incremental, sin recrear coleccion)..." -ForegroundColor Yellow
& $PythonCmd -m app.embed_index

Write-Host ""
Write-Host "🚀 [4/5] Ejecutando smoke tests de retrieval..." -ForegroundColor Yellow
& $PythonCmd scripts/smoke_test_retrieval.py

Write-Host ""
Write-Host "🚀 [5/5] Ejecutando preguntas de aceptación..." -ForegroundColor Yellow
& $PythonCmd scripts/acceptance_questions.py

Write-Host ""
Write-Host "==================================================" -ForegroundColor Green
Write-Host "✅ Proceso completado. Todo listo e indexado en Qdrant." -ForegroundColor Green
Write-Host "Levanta FastAPI con:" -ForegroundColor Green
Write-Host "  .\start_api.ps1" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
