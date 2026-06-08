#!/bin/bash

# Levanta la API FastAPI del RAG en el puerto que consume Chat.aspx.
set -e

PYTHON_CMD="python"

if [ -f "./.venv/Scripts/python.exe" ]; then
    PYTHON_CMD="./.venv/Scripts/python.exe"
elif [ -f "./.venv/bin/python" ]; then
    PYTHON_CMD="./.venv/bin/python"
elif command -v python3 &>/dev/null; then
    PYTHON_CMD="python3"
fi

echo "Usando Python: $PYTHON_CMD"
echo "Endpoint: http://127.0.0.1:8000"
echo "Health:   http://127.0.0.1:8000/health"
echo ""
echo "Si Qdrant no esta levantado en http://localhost:6333, /health saldra degraded."
echo "LLM local: Ollama en http://127.0.0.1:11434, modelo qwen3.5:4b."
echo "Pulsa Ctrl+C para detener FastAPI."
echo ""

export RAG_LLM_ENABLED="${RAG_LLM_ENABLED:-true}"
export RAG_LLM_BASE_URL="${RAG_LLM_BASE_URL:-http://127.0.0.1:11434}"
export RAG_LLM_API_KEY="${RAG_LLM_API_KEY:-ollama}"
export RAG_LLM_MODEL="${RAG_LLM_MODEL:-qwen3.5:4b}"
export RAG_LLM_TIMEOUT_SECONDS="${RAG_LLM_TIMEOUT_SECONDS:-90}"
export RAG_LLM_MAX_TOKENS="${RAG_LLM_MAX_TOKENS:-300}"
export RAG_LLM_MAX_CONTEXT_CHARS="${RAG_LLM_MAX_CONTEXT_CHARS:-1800}"

"$PYTHON_CMD" -m uvicorn app.api:app --host "${RAG_API_HOST:-127.0.0.1}" --port "${RAG_API_PORT:-8000}"
