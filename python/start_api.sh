#!/bin/bash

# Levanta Qdrant (Docker) y la API FastAPI del RAG.
set -e

# ── Qdrant (base de datos vectorial) ────────────────────────────────────────
echo "── [0] Qdrant ──────────────────────────────────────────────────────────"
mkdir -p ./qdrant_storage

if docker ps --format '{{.Names}}' 2>/dev/null | grep -q "^qdrant$"; then
    echo "Qdrant ya está en ejecución en http://localhost:6333"
elif docker ps -a --format '{{.Names}}' 2>/dev/null | grep -q "^qdrant$"; then
    echo "Arrancando contenedor Qdrant existente..."
    docker start qdrant
    echo "Qdrant listo en http://localhost:6333"
else
    echo "Creando y arrancando contenedor Qdrant..."
    docker run -d \
        --name qdrant \
        --restart unless-stopped \
        -p 6333:6333 \
        -p 6334:6334 \
        -v "$(pwd)/qdrant_storage:/qdrant/storage:z" \
        qdrant/qdrant
    echo "Qdrant listo en http://localhost:6333"
fi
echo ""

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

echo "── [1] FastAPI ─────────────────────────────────────────────────────────"
echo "Usando Python : $PYTHON_CMD"
echo "Endpoint      : http://127.0.0.1:8000"
echo "Health        : http://127.0.0.1:8000/health"
echo "LLM local     : Ollama en http://127.0.0.1:11434, modelo qwen3.5:4b"
echo "Pulsa Ctrl+C para detener FastAPI (Qdrant seguirá corriendo en Docker)."
echo ""

# ── Detectar LLM (remoto preferido) ──────────────────────────────────────────
REMOTE_LLM_URL="http://10.17.159.197:11434"
REMOTE_MODEL="qwen3:30b-a3b-instruct-2507-q4_K_M"
LOCAL_LLM_URL="http://127.0.0.1:11434"
LOCAL_MODEL="qwen3.5:4b"

echo "   Detectando LLM disponible..."
if curl -sf --max-time 3 "$REMOTE_LLM_URL/api/tags" > /dev/null 2>&1; then
    echo "   LLM remoto OK: $REMOTE_LLM_URL  →  $REMOTE_MODEL"
    DETECTED_LLM_URL="$REMOTE_LLM_URL"
    DETECTED_MODEL="$REMOTE_MODEL"
else
    echo "   LLM remoto no disponible, usando local: $LOCAL_LLM_URL  →  $LOCAL_MODEL"
    DETECTED_LLM_URL="$LOCAL_LLM_URL"
    DETECTED_MODEL="$LOCAL_MODEL"
fi
echo ""

export RAG_LLM_ENABLED="${RAG_LLM_ENABLED:-true}"
export RAG_LLM_BASE_URL="${RAG_LLM_BASE_URL:-$DETECTED_LLM_URL}"
export RAG_LLM_API_KEY="${RAG_LLM_API_KEY:-ollama}"
export RAG_LLM_MODEL="${RAG_LLM_MODEL:-$DETECTED_MODEL}"
export RAG_LLM_TIMEOUT_SECONDS="${RAG_LLM_TIMEOUT_SECONDS:-90}"
export RAG_LLM_MAX_TOKENS="${RAG_LLM_MAX_TOKENS:-300}"
export RAG_LLM_MAX_CONTEXT_CHARS="${RAG_LLM_MAX_CONTEXT_CHARS:-1800}"

"$PYTHON_CMD" -m uvicorn app.api:app --host 127.0.0.1 --port 8000
