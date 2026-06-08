"""Generación opcional OpenAI-compatible para /query.

El módulo nunca es obligatorio para operar el RAG: si el LLM está desactivado,
mal configurado o falla, se conserva la respuesta extractiva.
"""
from __future__ import annotations

from urllib.parse import urlparse
from typing import Any, Dict, List, Tuple

from . import config
from .models import Source
from .retrieval import build_extractive_answer


def _is_configured() -> tuple[bool, str | None]:
    if not config.LLM_ENABLED:
        return False, "RAG_LLM_ENABLED no está activo"
    if not config.LLM_BASE_URL:
        return False, "RAG_LLM_BASE_URL no está configurado"
    if not config.LLM_API_KEY:
        return False, "RAG_LLM_API_KEY no está configurado"
    if not config.LLM_MODEL:
        return False, "RAG_LLM_MODEL no está configurado"
    return True, None


def _source_text(src: Source) -> str:
    text = src.text or src.text_preview or ""
    return " ".join(text.split())


def _build_numbered_context(sources: List[Source]) -> tuple[str, int]:
    parts: list[str] = []
    used = 0
    max_chars = max(1_000, config.LLM_MAX_CONTEXT_CHARS)

    for i, src in enumerate(sources, start=1):
        title = src.title or src.source_name or src.domain or "Fuente"
        url = src.url or ""
        body = _source_text(src)
        if not body:
            continue

        header = f"[{i}] {title}\nDominio: {src.domain or '-'}\nURL: {url}\nTexto: "
        remaining = max_chars - used - len(header)
        if remaining <= 0:
            break

        chunk = body[:remaining]
        parts.append(header + chunk)
        used += len(header) + len(chunk)
        if used >= max_chars:
            break

    return "\n\n".join(parts), used


def _is_ollama_base_url(base_url: str | None) -> bool:
    if not base_url:
        return False
    parsed = urlparse(base_url)
    return parsed.port == 11434 or "ollama" in parsed.netloc.lower()


def _ollama_chat_completion(messages: list[dict[str, str]]) -> str:
    import requests

    base = (config.LLM_BASE_URL or "http://127.0.0.1:11434").rstrip("/")
    if base.endswith("/v1"):
        base = base[:-3]

    response = requests.post(
        f"{base}/api/chat",
        json={
            "model": config.LLM_MODEL,
            "messages": messages,
            "stream": False,
            "think": False,
            "options": {
                "temperature": 0.2,
                "num_predict": config.LLM_MAX_TOKENS,
            },
        },
        timeout=config.LLM_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    data = response.json()
    return ((data.get("message") or {}).get("content") or "").strip()


def _openai_chat_completion(messages: list[dict[str, str]]) -> str:
    from openai import OpenAI

    client = OpenAI(
        base_url=config.LLM_BASE_URL,
        api_key=config.LLM_API_KEY,
        timeout=config.LLM_TIMEOUT_SECONDS,
    )
    completion = client.chat.completions.create(
        model=config.LLM_MODEL,
        temperature=0.2,
        max_tokens=config.LLM_MAX_TOKENS,
        messages=messages,
    )
    return (completion.choices[0].message.content or "").strip()


def generate_answer(
    question: str,
    sources: List[Source],
) -> Tuple[str, str, Dict[str, Any]]:
    """Devuelve `(answer, answer_mode, diagnostics)`.

    `answer_mode` es `llm` solo si la síntesis generativa finaliza bien. En
    cualquier otro caso se devuelve la respuesta extractiva.
    """
    diagnostics: Dict[str, Any] = {
        "llm_enabled": config.LLM_ENABLED,
        "llm_model": config.LLM_MODEL,
    }

    configured, reason = _is_configured()
    if not configured:
        diagnostics["fallback_reason"] = reason
        return build_extractive_answer(question, sources), "extractive", diagnostics

    context, context_chars = _build_numbered_context(sources)
    diagnostics["context_chars"] = context_chars
    if not context:
        diagnostics["fallback_reason"] = "sin contexto recuperado"
        return build_extractive_answer(question, sources), "extractive", diagnostics

    try:
        messages = [
            {
                "role": "system",
                "content": (
                    "Eres un asistente RAG sobre Canarias. Responde en español "
                    "usando exclusivamente el contexto proporcionado. No inventes "
                    "datos. Si el contexto no basta, dilo claramente. Cita las "
                    "fuentes relevantes con su número entre corchetes, por ejemplo [1]. "
                    "No muestres razonamiento interno; da solo la respuesta final."
                ),
            },
            {
                "role": "user",
                "content": (
                    f"Pregunta:\n{question}\n\n"
                    f"Contexto recuperado:\n{context}\n\n"
                    "Redacta una respuesta breve, precisa y con citas."
                ),
            },
        ]
        if _is_ollama_base_url(config.LLM_BASE_URL):
            diagnostics["llm_provider"] = "ollama"
            answer = _ollama_chat_completion(messages)
        else:
            diagnostics["llm_provider"] = "openai_compatible"
            answer = _openai_chat_completion(messages)
        if not answer:
            raise RuntimeError("respuesta vacía del LLM")
        return answer, "llm", diagnostics
    except Exception as exc:
        diagnostics["fallback_reason"] = "error_llm"
        diagnostics["llm_error"] = str(exc)
        return build_extractive_answer(question, sources), "extractive", diagnostics
