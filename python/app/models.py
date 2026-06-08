"""Modelos Pydantic compartidos por el pipeline RAG."""
from __future__ import annotations

from typing import Any, Dict, List, Literal, Optional

from pydantic import BaseModel, Field


class DomainMetadata(BaseModel):
    """Metadatos a nivel de dominio (constantes para todas las páginas de ese sitio)."""

    domain: str
    domain_slug: Optional[str] = None
    source_name: Optional[str] = None
    source_type: Optional[str] = None
    category: Optional[str] = None
    region: Optional[str] = None
    island: Optional[str] = None
    language: Optional[str] = None
    license: Optional[str] = None
    topics: List[str] = Field(default_factory=list)
    description: Optional[str] = None
    reliability: Optional[str] = None
    notes: Optional[str] = None

    model_config = {"extra": "allow"}


class PageMetadata(BaseModel):
    """Metadatos por página, generados por el crawler de ASP.NET."""

    job: Optional[str] = None
    domain: str
    domain_slug: Optional[str] = None
    page_number: Optional[int] = None
    url: str
    title: Optional[str] = None
    file: Optional[str] = None
    crawled_at: Optional[str] = None
    chars: Optional[int] = None
    sha256: Optional[str] = None
    quality: Optional[str] = None
    duplicate_of: Optional[str] = None
    depth: Optional[int] = None

    model_config = {"extra": "allow"}


class Document(BaseModel):
    """Documento materializado a partir de un .txt + su .metadata.json."""

    source_id: str
    text: str
    txt_path: str
    domain_metadata: DomainMetadata
    page_metadata: PageMetadata


class Chunk(BaseModel):
    """Fragmento listo para indexar."""

    chunk_id: str
    source_id: str
    text: str
    metadata: dict[str, Any] = Field(default_factory=dict)


# --- API ------------------------------------------------------------------


class QueryRequest(BaseModel):
    question: str = Field(..., min_length=1)
    top_k: int = Field(default=5, ge=1, le=50)


class Source(BaseModel):
    score: float
    title: Optional[str] = None
    url: Optional[str] = None
    domain: Optional[str] = None
    source_name: Optional[str] = None
    text_preview: str
    # Texto completo del chunk, usado internamente por generation.py. No se
    # serializa en /query para mantener compacto el contrato con WebForms.
    text: Optional[str] = Field(default=None, exclude=True)
    # True cuando el resultado proviene del fallback global (la búsqueda
    # filtrada por dominio detectado no llenó top_k). En filtro duro
    # (--domain) este flag nunca debe aparecer porque no hay fallback.
    is_fallback: bool = False


class QueryResponse(BaseModel):
    answer: str
    sources: List[Source]
    answer_mode: Optional[Literal["llm", "extractive"]] = None
    diagnostics: Optional[Dict[str, Any]] = None
