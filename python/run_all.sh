#!/bin/bash

# Script para ejecutar todo el flujo del RAG de manera secuencial.
# Funciona tanto en WSL/Linux como en Git Bash en Windows.

# Salir inmediatamente si ocurre un erro
set -e

# Detectar el ejecutable de Python correcto
PYTHON_CMD="python"

if [ -f "./.venv/Scripts/python.exe" ]; then
    # Entorno virtual de Windows (corriendo en Git Bash o WSL)
    PYTHON_CMD="./.venv/Scripts/python.exe"
elif [ -f "./.venv/bin/python" ]; then
    # Entorno virtual de Linux / WSL nativo
    PYTHON_CMD="./.venv/bin/python"
elif command -v python3 &>/dev/null; then
    PYTHON_CMD="python3"
fi

echo "=================================================="
echo "Usando ejecutable de Python: $PYTHON_CMD"
echo "=================================================="
echo ""
echo "Corpus canonico: ../App_Data/crawlings  (relativo a la raiz del .NET)"
echo ""

echo "🚀 [1/5] Validando el corpus..."
"$PYTHON_CMD" -m app.validate_corpus

echo ""
echo "🚀 [2/5] Generando chunks (solo paginas nuevas/cambiadas)..."
"$PYTHON_CMD" -m app.chunk --incremental

echo ""
echo "🚀 [3/5] Indexando en Qdrant (upsert incremental, sin recrear coleccion)..."
"$PYTHON_CMD" -m app.embed_index

echo ""
echo "🚀 [4/5] Ejecutando smoke tests de retrieval..."
"$PYTHON_CMD" scripts/smoke_test_retrieval.py

echo ""
echo "🚀 [5/5] Ejecutando preguntas de aceptación..."
"$PYTHON_CMD" scripts/acceptance_questions.py

echo ""
echo "=================================================="
echo "✅ Proceso completado. Todo listo e indexado en Qdrant."
echo "Levanta FastAPI con:"
echo "  ./start_api.sh"
echo "=================================================="
