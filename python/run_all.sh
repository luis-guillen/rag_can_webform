#!/bin/bash

# Script para ejecutar todo el flujo del RAG de manera secuencial.
# Funciona tanto en WSL/Linux como en Git Bash en Windows.

set -e

# ── Python ────────────────────────────────────────────────────────────────────
PYTHON_CMD="python"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SIBLING_VENV="$SCRIPT_DIR/../../rag_can_python/.venv"

if [ -f "./.venv/Scripts/python.exe" ]; then
    PYTHON_CMD="./.venv/Scripts/python.exe"
elif [ -f "./.venv/bin/python" ]; then
    PYTHON_CMD="./.venv/bin/python"
elif [ -f "$SIBLING_VENV/Scripts/python.exe" ]; then
    PYTHON_CMD="$SIBLING_VENV/Scripts/python.exe"
elif [ -f "$SIBLING_VENV/bin/python" ]; then
    PYTHON_CMD="$SIBLING_VENV/bin/python"
elif command -v python3 &>/dev/null; then
    PYTHON_CMD="python3"
fi

echo "=================================================="
echo "Usando ejecutable de Python: $PYTHON_CMD"
echo "=================================================="
echo ""
echo "Corpus canonico: ../App_Data/crawlings  (relativo a la raiz del .NET)"
echo ""

# ── [0] Qdrant ────────────────────────────────────────────────────────────────
echo "🚀 [0/5] Asegurando Qdrant (Docker)..."
mkdir -p ./qdrant_storage
if docker ps --format '{{.Names}}' 2>/dev/null | grep -q "^qdrant$"; then
    echo "   Qdrant ya en ejecucion en http://localhost:6333"
elif docker ps -a --format '{{.Names}}' 2>/dev/null | grep -q "^qdrant$"; then
    echo "   Arrancando contenedor Qdrant existente..."
    docker start qdrant
    echo "   Qdrant listo en http://localhost:6333"
else
    echo "   Creando contenedor Qdrant..."
    docker run -d \
        --name qdrant \
        --restart unless-stopped \
        -p 6333:6333 \
        -p 6334:6334 \
        -v "$(pwd)/qdrant_storage:/qdrant/storage:z" \
        qdrant/qdrant
    echo "   Qdrant listo en http://localhost:6333"
fi
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
echo "🚀 [5/5] Ejecutando preguntas de aceptacion..."
"$PYTHON_CMD" scripts/acceptance_questions.py

echo ""
echo "=================================================="
echo "✅ Proceso completado. Todo listo e indexado en Qdrant."
echo "Levanta FastAPI con:"
echo "  ./start_api.sh"
echo "=================================================="
