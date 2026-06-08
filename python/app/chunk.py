"""Genera chunks a partir del corpus y los persiste en data/chunks.jsonl.

Usa RecursiveCharacterTextSplitter de langchain-text-splitters cuando esté
disponible (chunking respetuoso con párrafos), con un fallback propio si no.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
import unicodedata
from pathlib import Path
from typing import Iterable, List, Optional

from tqdm import tqdm

from . import config
from .corpus_utils import iter_corpus_entries, load_document
from .models import Chunk, Document

try:
    from langchain_text_splitters import RecursiveCharacterTextSplitter

    _HAS_LANGCHAIN = True
except ImportError:  # pragma: no cover - fallback
    _HAS_LANGCHAIN = False


def _build_splitter(chunk_size: int, chunk_overlap: int):
    if _HAS_LANGCHAIN:
        # Separadores conservadores: solo párrafos, líneas y palabras. NO
        # cortamos por frase (". ", "! ", "? ") ni por coma/punto-y-coma para
        # evitar microchunks en páginas con listas o cabeceras cortas.
        return RecursiveCharacterTextSplitter(
            chunk_size=chunk_size,
            chunk_overlap=chunk_overlap,
            length_function=len,
            separators=["\n\n", "\n", " ", ""],
        )
    return None


def _simple_split(text: str, chunk_size: int, chunk_overlap: int) -> List[str]:
    """Fallback: corte por párrafos con ventana deslizante por caracteres."""
    paragraphs = [p.strip() for p in text.split("\n\n") if p.strip()]
    chunks: List[str] = []
    buf = ""
    for p in paragraphs:
        if len(buf) + len(p) + 2 <= chunk_size:
            buf = f"{buf}\n\n{p}" if buf else p
        else:
            if buf:
                chunks.append(buf)
            if len(p) <= chunk_size:
                buf = p
            else:
                # Párrafo enorme: ventana deslizante
                start = 0
                while start < len(p):
                    end = min(start + chunk_size, len(p))
                    chunks.append(p[start:end])
                    if end == len(p):
                        break
                    start = end - chunk_overlap
                buf = ""
    if buf:
        chunks.append(buf)

    # Aplicar overlap entre chunks adyacentes
    if chunk_overlap > 0 and len(chunks) > 1:
        with_overlap: List[str] = [chunks[0]]
        for prev, curr in zip(chunks, chunks[1:]):
            tail = prev[-chunk_overlap:] if len(prev) > chunk_overlap else prev
            with_overlap.append(f"{tail}\n\n{curr}")
        return with_overlap
    return chunks


def split_text(text: str, chunk_size: int, chunk_overlap: int) -> List[str]:
    splitter = _build_splitter(chunk_size, chunk_overlap)
    if splitter is not None:
        return [c for c in splitter.split_text(text) if c.strip()]
    return [c for c in _simple_split(text, chunk_size, chunk_overlap) if c.strip()]


def _chunk_id(source_id: str, index: int, text: str) -> str:
    digest = hashlib.sha1(f"{source_id}:{index}:{text[:64]}".encode("utf-8")).hexdigest()[:16]
    return f"{source_id[:24]}-{index:04d}-{digest}"


def _payload_for(doc: Document) -> dict:
    """Aplana metadata para que sea trivial buscar/filtrar en Qdrant."""
    dom = doc.domain_metadata
    page = doc.page_metadata
    return {
        "url": page.url,
        "title": page.title,
        "domain": dom.domain,
        "domain_slug": dom.domain_slug,
        "source_name": dom.source_name,
        "source_type": dom.source_type,
        "category": dom.category,
        "region": dom.region,
        "island": dom.island,
        "language": dom.language,
        "license": dom.license,
        "topics": dom.topics,
        "file": page.file,
        "depth": page.depth,
        "source_id": doc.source_id,
    }


def _normalize(s: Optional[str]) -> str:
    """Minúsculas + plegado de acentos (NFKD), para comparar URL y título."""
    if not s:
        return ""
    s = s.lower()
    s = unicodedata.normalize("NFKD", s)
    return "".join(c for c in s if not unicodedata.combining(c))


def is_noisy(url: Optional[str], title: Optional[str], patterns: List[str]) -> Optional[str]:
    """Devuelve el patrón que ha hecho match o None si el doc no parece ruido.

    Usa límites de palabra (\\b) en regex, así "politica" matchea
    "/politica-de-privacidad/" o "Política de cookies" pero NO "apolitical".
    """
    haystack = f"{_normalize(url)} {_normalize(title)}"
    for pat in patterns:
        norm_pat = _normalize(pat)
        if not norm_pat:
            continue
        if re.search(rf"\b{re.escape(norm_pat)}\b", haystack):
            return pat
    return None


def _useful_chars(text: str) -> int:
    """Cuenta caracteres no-whitespace para descartar chunks que parecen llenos
    pero son en su mayoría espacios/saltos de línea."""
    return sum(1 for ch in text if not ch.isspace())


def chunk_document(doc: Document, chunk_size: int, chunk_overlap: int) -> Iterable[Chunk]:
    pieces = split_text(doc.text, chunk_size, chunk_overlap)
    base_payload = _payload_for(doc)
    out_index = 0
    for piece in pieces:
        if _useful_chars(piece) < config.MIN_CHUNK_CHARS:
            continue
        cid = _chunk_id(doc.source_id, out_index, piece)
        payload = dict(base_payload)
        payload["chunk_id"] = cid
        yield Chunk(
            chunk_id=cid,
            source_id=doc.source_id,
            text=piece,
            metadata=payload,
        )
        out_index += 1


def _needs_index(meta_path: Path) -> bool:
    """Devuelve True si el sidecar indica que la página necesita re-indexarse."""
    try:
        with meta_path.open("r", encoding="utf-8") as f:
            raw = json.load(f)
        return bool(raw.get("page_metadata", {}).get("needs_index", False))
    except Exception:
        return False


def _get_url(meta_path: Path) -> Optional[str]:
    """Extrae la URL del sidecar, o None si no está disponible."""
    try:
        with meta_path.open("r", encoding="utf-8") as f:
            raw = json.load(f)
        return raw.get("page_metadata", {}).get("url")
    except Exception:
        return None


def build_chunks(
    corpus_dir: Path,
    output_path: Path,
    chunk_size: int,
    chunk_overlap: int,
    max_doc_chars: int,
    *,
    include_noisy: bool = False,
    noise_patterns: Optional[List[str]] = None,
) -> dict:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    docs_ok = docs_skipped = docs_oversized = docs_noisy = chunks_total = 0
    oversized_examples: list[str] = []
    noisy_examples: list[str] = []
    patterns = noise_patterns if noise_patterns is not None else config.NOISE_PATTERNS

    entries = list(iter_corpus_entries(corpus_dir))
    with output_path.open("w", encoding="utf-8") as out:
        for entry in tqdm(entries, desc="Chunking", unit="doc"):
            doc = load_document(entry)
            if doc is None:
                docs_skipped += 1
                continue
            if len(doc.text) > max_doc_chars:
                docs_oversized += 1
                if len(oversized_examples) < 10:
                    oversized_examples.append(
                        f"{entry.txt_path.name} ({len(doc.text):,} chars)"
                    )
                continue
            if not include_noisy:
                matched = is_noisy(doc.page_metadata.url, doc.page_metadata.title, patterns)
                if matched:
                    docs_noisy += 1
                    if len(noisy_examples) < 10:
                        noisy_examples.append(
                            f"[{matched}] {doc.page_metadata.url or doc.page_metadata.title or entry.txt_path.name}"
                        )
                    continue
            docs_ok += 1
            for chunk in chunk_document(doc, chunk_size, chunk_overlap):
                out.write(
                    json.dumps(chunk.model_dump(), ensure_ascii=False) + "\n"
                )
                chunks_total += 1

    return {
        "docs_processed": docs_ok,
        "docs_skipped_invalid": docs_skipped,
        "docs_skipped_oversized": docs_oversized,
        "docs_skipped_noisy": docs_noisy,
        "oversized_examples": oversized_examples,
        "noisy_examples": noisy_examples,
        "noise_patterns": patterns,
        "noise_filter_enabled": not include_noisy,
        "chunks_written": chunks_total,
        "output": str(output_path),
        "splitter": "langchain.RecursiveCharacterTextSplitter" if _HAS_LANGCHAIN else "builtin",
        "chunk_size": chunk_size,
        "chunk_overlap": chunk_overlap,
        "max_doc_chars": max_doc_chars,
        "min_chunk_chars": config.MIN_CHUNK_CHARS,
    }


def build_chunks_incremental(
    corpus_dir: Path,
    output_path: Path,
    chunk_size: int,
    chunk_overlap: int,
    max_doc_chars: int,
    *,
    include_noisy: bool = False,
    noise_patterns: Optional[List[str]] = None,
) -> dict:
    """Modo incremental: solo re-chunkea páginas con needs_index=true.

    Carga el chunks.jsonl existente, descarta los chunks de las URLs que han
    cambiado, re-chunkea esas páginas y escribe el resultado fusionado.
    Con embed_index sin --recreate, Qdrant recibe solo los chunks nuevos/modificados.
    Los chunks huérfanos de páginas modificadas permanecen en Qdrant hasta el
    próximo rebuild completo (--full), que se recomienda hacer periódicamente.
    """
    output_path.parent.mkdir(parents=True, exist_ok=True)
    patterns = noise_patterns if noise_patterns is not None else config.NOISE_PATTERNS

    # 1. Determinar qué URLs necesitan re-indexarse
    entries = list(iter_corpus_entries(corpus_dir))
    changed_entries = [e for e in entries if _needs_index(e.meta_path)]
    changed_urls: set[str] = set()
    for e in changed_entries:
        url = _get_url(e.meta_path)
        if url:
            changed_urls.add(url)

    print(f"[chunk:incremental] {len(changed_entries)} docs con needs_index=true de {len(entries)} totales")

    # 2. Cargar chunks existentes y descartar los de URLs cambiadas
    kept_chunks: list[str] = []
    kept_count = 0
    if output_path.exists():
        with output_path.open("r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if not line:
                    continue
                try:
                    obj = json.loads(line)
                    url = obj.get("metadata", {}).get("url", "")
                    if url not in changed_urls:
                        kept_chunks.append(line)
                        kept_count += 1
                except Exception:
                    kept_chunks.append(line)
                    kept_count += 1
    print(f"[chunk:incremental] {kept_count} chunks anteriores conservados, {len(changed_urls)} URLs a re-chunkear")

    # 3. Re-chunkear solo los documentos cambiados
    docs_ok = docs_skipped = docs_oversized = docs_noisy = new_chunks = 0
    oversized_examples: list[str] = []
    noisy_examples: list[str] = []
    new_lines: list[str] = []

    for entry in tqdm(changed_entries, desc="Chunking (incremental)", unit="doc"):
        doc = load_document(entry)
        if doc is None:
            docs_skipped += 1
            continue
        if len(doc.text) > max_doc_chars:
            docs_oversized += 1
            if len(oversized_examples) < 10:
                oversized_examples.append(f"{entry.txt_path.name} ({len(doc.text):,} chars)")
            continue
        if not include_noisy:
            matched = is_noisy(doc.page_metadata.url, doc.page_metadata.title, patterns)
            if matched:
                docs_noisy += 1
                if len(noisy_examples) < 10:
                    noisy_examples.append(
                        f"[{matched}] {doc.page_metadata.url or doc.page_metadata.title or entry.txt_path.name}"
                    )
                continue
        docs_ok += 1
        for chunk in chunk_document(doc, chunk_size, chunk_overlap):
            new_lines.append(json.dumps(chunk.model_dump(), ensure_ascii=False))
            new_chunks += 1

    # 4. Escribir resultado fusionado
    with output_path.open("w", encoding="utf-8") as out:
        for line in kept_chunks:
            out.write(line + "\n")
        for line in new_lines:
            out.write(line + "\n")

    return {
        "mode": "incremental",
        "docs_changed": len(changed_entries),
        "docs_processed": docs_ok,
        "docs_skipped_invalid": docs_skipped,
        "docs_skipped_oversized": docs_oversized,
        "docs_skipped_noisy": docs_noisy,
        "oversized_examples": oversized_examples,
        "noisy_examples": noisy_examples,
        "chunks_kept": kept_count,
        "chunks_new": new_chunks,
        "chunks_total": kept_count + new_chunks,
        "output": str(output_path),
        "splitter": "langchain.RecursiveCharacterTextSplitter" if _HAS_LANGCHAIN else "builtin",
        "chunk_size": chunk_size,
        "chunk_overlap": chunk_overlap,
        "max_doc_chars": max_doc_chars,
        "min_chunk_chars": config.MIN_CHUNK_CHARS,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Genera chunks RAG desde el corpus ASP.NET.")
    parser.add_argument("--corpus", default=str(config.CORPUS_DIR))
    parser.add_argument("--output", default=str(config.CHUNKS_FILE))
    parser.add_argument("--chunk-size", type=int, default=config.CHUNK_SIZE)
    parser.add_argument("--chunk-overlap", type=int, default=config.CHUNK_OVERLAP)
    parser.add_argument(
        "--max-doc-chars",
        type=int,
        default=config.MAX_DOC_CHARS,
        help="Skip documentos con más caracteres (probable binario mal interpretado).",
    )
    parser.add_argument(
        "--include-noisy",
        action="store_true",
        help="Indexar también páginas de aviso legal, cookies, login, tags, feed, etc.",
    )
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument(
        "--incremental",
        action="store_true",
        default=True,
        help="(Por defecto) Solo re-chunkea páginas con needs_index=true y fusiona con chunks existentes.",
    )
    mode.add_argument(
        "--full",
        action="store_true",
        help="Rebuild completo: re-chunkea TODO el corpus y sobreescribe chunks.jsonl.",
    )
    args = parser.parse_args()

    corpus = Path(args.corpus)
    if not corpus.exists():
        print(f"ERROR: corpus not found: {corpus}", file=sys.stderr)
        sys.exit(2)

    if args.full:
        stats = build_chunks(
            corpus,
            Path(args.output),
            args.chunk_size,
            args.chunk_overlap,
            args.max_doc_chars,
            include_noisy=args.include_noisy,
        )
    else:
        stats = build_chunks_incremental(
            corpus,
            Path(args.output),
            args.chunk_size,
            args.chunk_overlap,
            args.max_doc_chars,
            include_noisy=args.include_noisy,
        )
    print(json.dumps(stats, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
