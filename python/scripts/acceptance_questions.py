"""Aceptación operativa del RAG con seis preguntas canónicas.

Ejecuta:
    python scripts/acceptance_questions.py

Requiere Qdrant indexado. Valida que cada pregunta recupere al menos una
fuente del dominio esperado.
"""
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from app import config  # noqa: E402
from app.retrieval import inspect_collection, search  # noqa: E402


GREEN = "\033[32m"
RED = "\033[31m"
DIM = "\033[2m"
RESET = "\033[0m"


CASES = [
    (
        "¿Qué es el Fondo de Arqueología del Museo Canario?",
        "elmuseocanario.com",
    ),
    (
        "¿Qué ofrece Canarias Azul?",
        "canarias-azul.iatext.ulpgc.es",
    ),
    (
        "¿Qué información hay sobre el litoral de Gran Canaria?",
        "canarias-azul.iatext.ulpgc.es",
    ),
    (
        "¿Qué es el Diccionario básico de canarismos?",
        "www.academiacanarialengua.org",
    ),
    (
        "¿Qué es Memoria de Lanzarote?",
        "memoriadelanzarote.com",
    ),
    (
        "¿Qué dice Izuran sobre los guanches?",
        "izuran.blogspot.com",
    ),
]


def main() -> int:
    info = inspect_collection()
    if not info.get("exists"):
        print(
            f"{RED}FAIL{RESET} La colección no existe. Ejecuta "
            "`python -m app.embed_index --recreate` primero."
        )
        return 2

    points = info.get("points_count") or 0
    if points <= 0:
        print(f"{RED}FAIL{RESET} La colección existe, pero no tiene puntos.")
        return 2

    print(f"Colección: {config.COLLECTION_NAME} ({points} puntos)")
    failures = 0

    for question, expected_domain in CASES:
        results = search(question, config.TOP_K_DEFAULT, auto_detect=True)
        domains = [r.domain for r in results]
        matched = any(r.domain == expected_domain for r in results)
        if matched:
            print(f"{GREEN}OK  {RESET}{expected_domain} <- {question}")
        else:
            failures += 1
            print(f"{RED}FAIL{RESET} {expected_domain} <- {question}")
            print(f"{DIM}    dominios recuperados={domains}{RESET}")

    if failures:
        print(f"\n{RED}{failures} pregunta(s) no recuperaron el dominio esperado.{RESET}")
        return 1

    print(f"\n{GREEN}ACEPTACION COMPLETADA.{RESET}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
