"""Evaluación automática del sistema RAG Canarias.

Ejecutar desde python/:
    python scripts/run_evaluation.py

Genera:
    evaluation/results.json   — resultados brutos + métricas
    evaluation/report.md      — informe en Markdown
    evaluation/tfg_tables.md  — tablas LaTeX para la memoria
"""
from __future__ import annotations

import json
import statistics
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

import requests

# ── Rutas ────────────────────────────────────────────────────────────────────

ROOT = Path(__file__).resolve().parent.parent        # → python/
DATA_FILE   = ROOT / "data" / "evaluation" / "questions.json"
OUT_DIR     = ROOT / "evaluation"
OUT_RESULTS = OUT_DIR / "results.json"
OUT_REPORT  = OUT_DIR / "report.md"
OUT_TABLES  = OUT_DIR / "tfg_tables.md"

API_URL  = "http://127.0.0.1:8000/query"
TOP_K    = 5
TIMEOUT  = 90

REJECTION_PHRASES = [
    "no se encontró",
    "sin evidencia",
    "no he encontrado",
    "no encontré",
    "no hay información",
    "no dispongo",
    "no tengo información",
    "corpus indexado no",
]

CATEGORY_ORDER   = ["retrieval", "synthesis", "multisource", "semantic", "reasoning"]
DIFFICULTY_ORDER = ["easy", "medium", "hard", "expert"]

# ── Helpers de métricas ───────────────────────────────────────────────────────

def recall_at_k(sources: list[dict], expected: list[str], k: int) -> int | None:
    if not expected:
        return None
    domains = [s.get("domain", "") for s in sources[:k]]
    return 1 if any(d in expected for d in domains) else 0


def mrr_score(sources: list[dict], expected: list[str]) -> float | None:
    if not expected:
        return None
    for i, s in enumerate(sources[:TOP_K], start=1):
        if s.get("domain", "") in expected:
            return 1.0 / i
    return 0.0


def is_rejected(answer: str) -> bool:
    low = (answer or "").lower()
    return any(p in low for p in REJECTION_PHRASES)


def safe_mean(values: list) -> float | None:
    filtered = [v for v in values if v is not None]
    return round(statistics.mean(filtered), 4) if filtered else None


def p90(values: list[float]) -> float | None:
    if not values:
        return None
    sorted_v = sorted(values)
    idx = min(int(len(sorted_v) * 0.9), len(sorted_v) - 1)
    return round(sorted_v[idx], 1)

# ── API call ─────────────────────────────────────────────────────────────────

def query_api(question: str) -> tuple[dict, float]:
    t0 = time.monotonic()
    try:
        r = requests.post(
            API_URL,
            json={"question": question, "top_k": TOP_K},
            timeout=TIMEOUT,
        )
        elapsed_ms = (time.monotonic() - t0) * 1000
        r.raise_for_status()
        return r.json(), elapsed_ms
    except Exception as exc:
        elapsed_ms = (time.monotonic() - t0) * 1000
        return {"error": str(exc)}, elapsed_ms

# ── Runner ────────────────────────────────────────────────────────────────────

def run_evaluation(questions: list[dict]) -> list[dict]:
    results: list[dict] = []
    n = len(questions)

    for i, q in enumerate(questions, start=1):
        prefix = f"[{i:2d}/{n}] Q{q['id']:02d} ({q['difficulty']:6s}/{q['category']:11s})"
        print(f"{prefix} ...", end=" ", flush=True)

        resp, elapsed_ms = query_api(q["question"])

        if "error" in resp:
            record = {
                "id": q["id"],
                "difficulty": q["difficulty"],
                "category": q["category"],
                "question": q["question"],
                "expected_sources": q["expected_sources"],
                "expected_keywords": q.get("expected_keywords", []),
                "error": resp["error"],
                "response_time_ms": round(elapsed_ms, 1),
                "recall_at_1": None,
                "recall_at_3": None,
                "recall_at_5": None,
                "mrr": None,
                "num_sources": 0,
                "has_sources": False,
                "rejected": False,
            }
            print(f"ERROR ({elapsed_ms:.0f} ms)")
        else:
            sources = resp.get("sources") or []
            answer  = resp.get("answer", "")

            r1 = recall_at_k(sources, q["expected_sources"], 1)
            r3 = recall_at_k(sources, q["expected_sources"], 3)
            r5 = recall_at_k(sources, q["expected_sources"], 5)
            mrr = mrr_score(sources, q["expected_sources"])

            record = {
                "id": q["id"],
                "difficulty": q["difficulty"],
                "category": q["category"],
                "question": q["question"],
                "expected_sources": q["expected_sources"],
                "expected_keywords": q.get("expected_keywords", []),
                "answer": answer,
                "answer_mode": resp.get("answer_mode"),
                "sources": sources,
                "response_time_ms": round(elapsed_ms, 1),
                "recall_at_1": r1,
                "recall_at_3": r3,
                "recall_at_5": r5,
                "mrr": mrr,
                "num_sources": len(sources),
                "has_sources": len(sources) > 0,
                "rejected": is_rejected(answer),
            }

            flag = "OK  " if r5 == 1 else ("N/A " if r5 is None else "MISS")
            mrr_str = f"{mrr:.2f}" if mrr is not None else " N/A"
            print(f"{flag} R@5={r5 if r5 is not None else '-'} MRR={mrr_str} ({elapsed_ms:.0f} ms)")

        results.append(record)

    return results

# ── Agregación de métricas ────────────────────────────────────────────────────

def aggregate(results: list[dict]) -> dict:
    all_ok = [r for r in results if "error" not in r]
    scored = [r for r in all_ok if r["recall_at_5"] is not None]

    latencies = [r["response_time_ms"] for r in all_ok]

    metrics: dict = {
        "total": len(results),
        "errors": len(results) - len(all_ok),
        "scored": len(scored),
        "recall_at_1": safe_mean([r["recall_at_1"] for r in scored]),
        "recall_at_3": safe_mean([r["recall_at_3"] for r in scored]),
        "recall_at_5": safe_mean([r["recall_at_5"] for r in scored]),
        "mrr": safe_mean([r["mrr"] for r in scored]),
        "latency_avg_ms": round(statistics.mean(latencies), 1) if latencies else None,
        "latency_max_ms": round(max(latencies), 1) if latencies else None,
        "latency_min_ms": round(min(latencies), 1) if latencies else None,
        "latency_p90_ms": p90(latencies),
        "pct_with_sources": round(sum(r["has_sources"] for r in all_ok) / len(all_ok) * 100, 1) if all_ok else None,
        "pct_without_sources": round(sum(not r["has_sources"] for r in all_ok) / len(all_ok) * 100, 1) if all_ok else None,
        "pct_rejected": round(sum(r["rejected"] for r in all_ok) / len(all_ok) * 100, 1) if all_ok else None,
    }

    # Por categoría
    by_cat: dict = {}
    for cat in CATEGORY_ORDER:
        grp = [r for r in scored if r["category"] == cat]
        lat_grp = [r["response_time_ms"] for r in all_ok if r["category"] == cat]
        by_cat[cat] = {
            "n": len(grp),
            "recall_at_5": safe_mean([r["recall_at_5"] for r in grp]),
            "mrr": safe_mean([r["mrr"] for r in grp]),
            "latency_avg_ms": round(statistics.mean(lat_grp), 1) if lat_grp else None,
        }
    metrics["by_category"] = by_cat

    # Por dificultad
    by_diff: dict = {}
    for diff in DIFFICULTY_ORDER:
        grp = [r for r in scored if r["difficulty"] == diff]
        lat_grp = [r["response_time_ms"] for r in all_ok if r["difficulty"] == diff]
        by_diff[diff] = {
            "n": len(grp),
            "recall_at_5": safe_mean([r["recall_at_5"] for r in grp]),
            "mrr": safe_mean([r["mrr"] for r in grp]),
            "latency_avg_ms": round(statistics.mean(lat_grp), 1) if lat_grp else None,
        }
    metrics["by_difficulty"] = by_diff

    # Casos destacados
    metrics["best_questions"]   = [r["id"] for r in scored if r["mrr"] == 1.0][:10]
    metrics["worst_questions"]  = [r["id"] for r in scored if r["recall_at_5"] == 0][:10]
    metrics["rejected_ids"]     = [r["id"] for r in all_ok if r["rejected"]]
    multisource_hits = []
    for r in all_ok:
        srcs = r.get("sources") or []
        domains = {s.get("domain") for s in srcs if s.get("domain")}
        if len(domains) > 1:
            multisource_hits.append(r["id"])
    metrics["multisource_hits"] = multisource_hits[:10]

    return metrics

# ── Generación del informe Markdown ──────────────────────────────────────────

def fmt_pct(v) -> str:
    if v is None:
        return "-"
    return f"{v * 100:.1f}%"


def fmt_dec(v, decimals: int = 4) -> str:
    if v is None:
        return "-"
    return f"{v:.{decimals}f}"


def fmt_ms(v) -> str:
    if v is None:
        return "-"
    return f"{v:.0f} ms"


def write_report(results: list[dict], metrics: dict, path: Path, ts: str) -> None:
    lines: list[str] = []

    lines += [
        "# Informe de Evaluación — RAG Canarias",
        "",
        f"**Generado:** {ts}  ",
        f"**API:** {API_URL}  ",
        f"**Top-K:** {TOP_K}",
        "",
        "---",
        "",
        "## Resumen ejecutivo",
        "",
        "| Métrica | Valor |",
        "|---------|-------|",
        f"| Total preguntas | {metrics['total']} |",
        f"| Preguntas con score (expected_sources ≠ ∅) | {metrics['scored']} |",
        f"| Errores de API | {metrics['errors']} |",
        f"| Recall@1 | {fmt_pct(metrics['recall_at_1'])} |",
        f"| Recall@3 | {fmt_pct(metrics['recall_at_3'])} |",
        f"| Recall@5 | {fmt_pct(metrics['recall_at_5'])} |",
        f"| MRR | {fmt_dec(metrics['mrr'])} |",
        f"| Latencia media | {fmt_ms(metrics['latency_avg_ms'])} |",
        f"| Latencia mínima | {fmt_ms(metrics['latency_min_ms'])} |",
        f"| Latencia máxima | {fmt_ms(metrics['latency_max_ms'])} |",
        f"| Latencia p90 | {fmt_ms(metrics['latency_p90_ms'])} |",
        f"| Respuestas con fuentes | {metrics['pct_with_sources']}% |" if metrics['pct_with_sources'] is not None else "| Respuestas con fuentes | - |",
        f"| Respuestas sin fuentes | {metrics['pct_without_sources']}% |" if metrics['pct_without_sources'] is not None else "| Respuestas sin fuentes | - |",
        f"| Respuestas rechazadas | {metrics['pct_rejected']}% |" if metrics['pct_rejected'] is not None else "| Respuestas rechazadas | - |",
        "",
        "---",
        "",
        "## Rendimiento por categoría",
        "",
        "| Categoría | N | Recall@5 | MRR | Lat. media |",
        "|-----------|---|---------|-----|-----------|",
    ]

    for cat in CATEGORY_ORDER:
        v = metrics["by_category"].get(cat, {})
        lines.append(
            f"| {cat} | {v.get('n', 0)} | {fmt_pct(v.get('recall_at_5'))} "
            f"| {fmt_dec(v.get('mrr'))} | {fmt_ms(v.get('latency_avg_ms'))} |"
        )

    lines += [
        "",
        "---",
        "",
        "## Rendimiento por dificultad",
        "",
        "| Dificultad | N | Recall@5 | MRR | Lat. media |",
        "|------------|---|---------|-----|-----------|",
    ]

    for diff in DIFFICULTY_ORDER:
        v = metrics["by_difficulty"].get(diff, {})
        lines.append(
            f"| {diff} | {v.get('n', 0)} | {fmt_pct(v.get('recall_at_5'))} "
            f"| {fmt_dec(v.get('mrr'))} | {fmt_ms(v.get('latency_avg_ms'))} |"
        )

    # Casos destacados
    lines += ["", "---", "", "## Casos destacados", ""]

    if metrics["best_questions"]:
        lines.append(f"**Mejor recuperación (MRR = 1.0):** preguntas {metrics['best_questions']}")
    if metrics["worst_questions"]:
        lines.append(f"**Sin recuperación (Recall@5 = 0):** preguntas {metrics['worst_questions']}")
    if metrics["rejected_ids"]:
        lines.append(f"**Rechazadas por falta de evidencia:** preguntas {metrics['rejected_ids']}")
    if metrics["multisource_hits"]:
        lines.append(f"**Respuestas multifuente exitosas:** preguntas {metrics['multisource_hits']}")

    # Preguntas más difíciles
    all_ok = [r for r in results if "error" not in r and r["recall_at_5"] == 0]
    if all_ok:
        lines += ["", "---", "", "## Preguntas con fallo de recuperación (Recall@5 = 0)", ""]
        for r in all_ok[:15]:
            lines.append(f"- **Q{r['id']:02d}** [{r['difficulty']}/{r['category']}] — {r['question']}")

    # Tabla completa
    lines += [
        "",
        "---",
        "",
        "## Resultados completos",
        "",
        "| ID | Dif. | Cat. | R@1 | R@3 | R@5 | MRR | Lat. (ms) | Fuentes |",
        "|----|------|------|-----|-----|-----|-----|-----------|---------|",
    ]

    for r in results:
        if "error" in r:
            lines.append(
                f"| Q{r['id']:02d} | {r['difficulty']} | {r['category']} "
                f"| ERR | ERR | ERR | ERR | {r['response_time_ms']:.0f} | ERROR |"
            )
        else:
            domains = ", ".join({s.get("domain", "-") for s in r.get("sources", [])})
            lines.append(
                f"| Q{r['id']:02d} | {r['difficulty']} | {r['category']} "
                f"| {r['recall_at_1'] if r['recall_at_1'] is not None else '-'} "
                f"| {r['recall_at_3'] if r['recall_at_3'] is not None else '-'} "
                f"| {r['recall_at_5'] if r['recall_at_5'] is not None else '-'} "
                f"| {fmt_dec(r['mrr'], 2)} "
                f"| {r['response_time_ms']:.0f} "
                f"| {domains} |"
            )

    path.write_text("\n".join(lines), encoding="utf-8")

# ── Tablas LaTeX para TFG ─────────────────────────────────────────────────────

def write_tfg_tables(metrics: dict, path: Path, ts: str) -> None:
    r1  = fmt_dec(metrics["recall_at_1"], 4) if metrics["recall_at_1"] is not None else "-"
    r3  = fmt_dec(metrics["recall_at_3"], 4) if metrics["recall_at_3"] is not None else "-"
    r5  = fmt_dec(metrics["recall_at_5"], 4) if metrics["recall_at_5"] is not None else "-"
    mrr = fmt_dec(metrics["mrr"], 4)         if metrics["mrr"]         is not None else "-"
    lat_avg = f"{metrics['latency_avg_ms']:.0f}" if metrics["latency_avg_ms"] is not None else "-"
    lat_p90 = f"{metrics['latency_p90_ms']:.0f}" if metrics["latency_p90_ms"] is not None else "-"
    pct_src = f"{metrics['pct_with_sources']:.1f}" if metrics["pct_with_sources"] is not None else "-"
    pct_rej = f"{metrics['pct_rejected']:.1f}"      if metrics["pct_rejected"]    is not None else "-"

    lines: list[str] = [
        f"% Tablas de evaluación — RAG Canarias",
        f"% Generado: {ts}",
        f"% Copiar directamente en el capítulo de evaluación del TFG.",
        f"% Requiere \\usepackage{{booktabs}} en el preámbulo.",
        "",
        "% ─────────────────────────────────────────────────────────────────────",
        "% TABLA 1: Métricas globales",
        "% ─────────────────────────────────────────────────────────────────────",
        "",
        "\\begin{table}[htbp]",
        "\\centering",
        "\\caption{Métricas globales de evaluación del sistema RAG Canarias}",
        "\\label{tab:eval_global}",
        "\\begin{tabular}{lc}",
        "\\toprule",
        "\\textbf{Métrica} & \\textbf{Valor} \\\\",
        "\\midrule",
        f"Recall@1  & {r1} \\\\",
        f"Recall@3  & {r3} \\\\",
        f"Recall@5  & {r5} \\\\",
        f"MRR       & {mrr} \\\\",
        "\\midrule",
        f"Latencia media (ms) & {lat_avg} \\\\",
        f"Latencia p90 (ms)   & {lat_p90} \\\\",
        "\\midrule",
        f"Respuestas con fuentes (\\%) & {pct_src} \\\\",
        f"Respuestas rechazadas (\\%)  & {pct_rej} \\\\",
        "\\bottomrule",
        "\\end{tabular}",
        "\\end{table}",
        "",
        "% ─────────────────────────────────────────────────────────────────────",
        "% TABLA 2: Métricas por tipo de pregunta (categoría)",
        "% ─────────────────────────────────────────────────────────────────────",
        "",
        "\\begin{table}[htbp]",
        "\\centering",
        "\\caption{Recall@5 y latencia media por tipo de pregunta}",
        "\\label{tab:eval_categoria}",
        "\\begin{tabular}{lcccc}",
        "\\toprule",
        "\\textbf{Tipo} & \\textbf{N} & \\textbf{Recall@5} & \\textbf{MRR} & \\textbf{Lat. media (ms)} \\\\",
        "\\midrule",
    ]

    cat_labels = {
        "retrieval":   "Recuperación directa",
        "synthesis":   "Síntesis",
        "multisource": "Multifuente",
        "semantic":    "Semántica",
        "reasoning":   "Razonamiento",
    }
    for cat in CATEGORY_ORDER:
        v = metrics["by_category"].get(cat, {})
        label = cat_labels.get(cat, cat)
        n    = v.get("n", 0)
        rc5  = fmt_dec(v.get("recall_at_5"), 4) if v.get("recall_at_5") is not None else "-"
        mrr_ = fmt_dec(v.get("mrr"), 4)         if v.get("mrr")         is not None else "-"
        lat_ = f"{v['latency_avg_ms']:.0f}"     if v.get("latency_avg_ms") is not None else "-"
        lines.append(f"{label} & {n} & {rc5} & {mrr_} & {lat_} \\\\")

    lines += [
        "\\bottomrule",
        "\\end{tabular}",
        "\\end{table}",
        "",
        "% ─────────────────────────────────────────────────────────────────────",
        "% TABLA 3: Métricas por nivel de dificultad",
        "% ─────────────────────────────────────────────────────────────────────",
        "",
        "\\begin{table}[htbp]",
        "\\centering",
        "\\caption{Recall@5 y latencia media por nivel de dificultad}",
        "\\label{tab:eval_dificultad}",
        "\\begin{tabular}{lcccc}",
        "\\toprule",
        "\\textbf{Nivel} & \\textbf{N} & \\textbf{Recall@5} & \\textbf{MRR} & \\textbf{Lat. media (ms)} \\\\",
        "\\midrule",
    ]

    diff_labels = {
        "easy":   "Fácil (Nivel 1)",
        "medium": "Medio (Nivel 2)",
        "hard":   "Difícil (Niveles 3--4)",
        "expert": "Experto (Nivel 5)",
    }
    for diff in DIFFICULTY_ORDER:
        v = metrics["by_difficulty"].get(diff, {})
        label = diff_labels.get(diff, diff)
        n    = v.get("n", 0)
        rc5  = fmt_dec(v.get("recall_at_5"), 4) if v.get("recall_at_5") is not None else "-"
        mrr_ = fmt_dec(v.get("mrr"), 4)         if v.get("mrr")         is not None else "-"
        lat_ = f"{v['latency_avg_ms']:.0f}"     if v.get("latency_avg_ms") is not None else "-"
        lines.append(f"{label} & {n} & {rc5} & {mrr_} & {lat_} \\\\")

    lines += [
        "\\bottomrule",
        "\\end{tabular}",
        "\\end{table}",
        "",
        "% Fin de tablas de evaluación",
    ]

    path.write_text("\n".join(lines), encoding="utf-8")

# ── Main ──────────────────────────────────────────────────────────────────────

def main() -> int:
    OUT_DIR.mkdir(parents=True, exist_ok=True)

    if not DATA_FILE.exists():
        print(f"ERROR: no se encuentra {DATA_FILE}", file=sys.stderr)
        print("Asegúrate de ejecutar desde python/ y de que el archivo de preguntas existe.", file=sys.stderr)
        return 2

    questions = json.loads(DATA_FILE.read_text(encoding="utf-8"))
    ts = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

    print(f"RAG Canarias — Evaluación automática")
    print(f"Preguntas: {len(questions)}  |  API: {API_URL}  |  Top-K: {TOP_K}")
    print(f"Inicio: {ts}")
    print("=" * 70)

    results  = run_evaluation(questions)
    metrics  = aggregate(results)

    print("=" * 70)
    print(f"\n  Recall@1 : {fmt_pct(metrics['recall_at_1'])}")
    print(f"  Recall@3 : {fmt_pct(metrics['recall_at_3'])}")
    print(f"  Recall@5 : {fmt_pct(metrics['recall_at_5'])}")
    print(f"  MRR      : {fmt_dec(metrics['mrr'])}")
    print(f"  Lat. avg : {fmt_ms(metrics['latency_avg_ms'])}")
    print(f"  Lat. p90 : {fmt_ms(metrics['latency_p90_ms'])}")
    print(f"  Con fuentes  : {metrics['pct_with_sources']}%")
    print(f"  Sin fuentes  : {metrics['pct_without_sources']}%")
    print(f"  Rechazadas   : {metrics['pct_rejected']}%")

    # Guardar results.json
    payload = {
        "generated_at": ts,
        "api_url": API_URL,
        "top_k": TOP_K,
        "metrics": metrics,
        "results": results,
    }
    OUT_RESULTS.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    write_report(results, metrics, OUT_REPORT, ts)
    write_tfg_tables(metrics, OUT_TABLES, ts)

    print(f"\n  Resultados → {OUT_RESULTS}")
    print(f"  Informe    → {OUT_REPORT}")
    print(f"  Tablas TFG → {OUT_TABLES}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
