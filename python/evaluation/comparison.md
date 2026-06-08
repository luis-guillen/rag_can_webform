# Comparativa de Evaluación — RAG Canarias

**Corpus:** 50 preguntas de patrimonio cultural canario  
**Recuperación:** Qdrant local · `intfloat/multilingual-e5-small` · Top-K=5  
**LLM remoto:** `qwen3:30b-a3b-instruct-2507-q4_K_M` (Dell Pro Max 10.17.159.197)  
**LLM local:** `qwen3.5:4b` (Ollama en localhost)

---

## Métricas globales

| Métrica | LLM remoto (30B) | LLM local (4B) | Δ |
|---------|-----------------|----------------|---|
| **Recall@1** | **72.0 %** | **72.0 %** | = |
| **Recall@3** | **92.0 %** | **92.0 %** | = |
| **Recall@5** | **100.0 %** | **100.0 %** | = |
| **MRR** | **0.8190** | **0.8190** | = |
| Latencia media | 2 715 ms | 2 711 ms | −4 ms |
| Latencia p90 | 3 780 ms | 3 835 ms | +55 ms |
| Respuestas con fuentes | 100.0 % | 100.0 % | = |
| Respuestas rechazadas | 0.0 % | 0.0 % | = |
| Errores de API | 0 | 0 | = |
| Respuestas texto distintas | — | 39 / 50 | — |

**Ejecución remoto:** 2026-06-08T20:09:24Z  
**Ejecución local:** 2026-06-08T20:34:58Z

---

## Explicación del resultado

Las métricas de recuperación (**Recall@K**, **MRR**) son **idénticas** en ambos runs porque dependen exclusivamente del pipeline Qdrant+embeddings, que no cambia entre configuraciones. El LLM solo influye en el texto de la respuesta generada.

Las 39/50 respuestas con texto diferente confirman que se utilizaron dos modelos distintos.  
Ejemplo representativo — Q09 *"¿Qué información contiene el blog Izuran?"*:
- **Remoto (30B):** *"...con enfoque en su origen amazigh [1]..."*  
- **Local (4B):** *"...con énfasis en su origen amazigh (beréber) [1]..."*

---

## Por tipo de pregunta

| Tipo | N | R@5 remoto | MRR remoto | Lat. remoto | R@5 local | MRR local | Lat. local |
|------|---|-----------|-----------|------------|----------|----------|-----------|
| Recuperación directa | 15 | 100.0 % | 0.8167 | 2 546 ms | 100.0 % | 0.8167 | 2 602 ms |
| Síntesis | 10 | 100.0 % | 0.8250 | 2 723 ms | 100.0 % | 0.8250 | 2 886 ms |
| Multifuente | 10 | 100.0 % | 0.9500 | 2 707 ms | 100.0 % | 0.9500 | 2 810 ms |
| Semántica | 8 | 100.0 % | 0.7396 | 3 111 ms | 100.0 % | 0.7396 | 2 429 ms |
| Razonamiento | 7 | 100.0 % | 0.7190 | 2 627 ms | 100.0 % | 0.7190 | 2 873 ms |

---

## Por dificultad

| Nivel | N | R@5 remoto | MRR remoto | Lat. remoto | R@5 local | MRR local | Lat. local |
|-------|---|-----------|-----------|------------|----------|----------|-----------|
| Fácil (Nivel 1) | 15 | 100.0 % | 0.8167 | 2 546 ms | 100.0 % | 0.8167 | 2 602 ms |
| Medio (Nivel 2) | 10 | 100.0 % | 0.8250 | 2 723 ms | 100.0 % | 0.8250 | 2 886 ms |
| Difícil (Niveles 3-4) | 18 | 100.0 % | 0.8565 | 2 886 ms | 100.0 % | 0.8565 | 2 640 ms |
| Experto (Nivel 5) | 7 | 100.0 % | 0.7190 | 2 627 ms | 100.0 % | 0.7190 | 2 873 ms |

---

## Tablas LaTeX (TFG)

```latex
% ─── Tabla comparativa global ───────────────────────────────────────────────
\begin{table}[htbp]
\centering
\caption{Métricas de evaluación del sistema RAG Canarias según configuración de LLM}
\label{tab:eval_comparativa}
\begin{tabular}{lccc}
\toprule
\textbf{Métrica} & \textbf{LLM remoto} & \textbf{LLM local} & \textbf{$\Delta$} \\
                 & \textit{qwen3:30b-a3b} & \textit{qwen3.5:4b} & \\
\midrule
Recall@1   & 72.0\,\% & 72.0\,\% & 0 \\
Recall@3   & 92.0\,\% & 92.0\,\% & 0 \\
Recall@5   & \textbf{100.0\,\%} & \textbf{100.0\,\%} & 0 \\
MRR        & \textbf{0.8190} & \textbf{0.8190} & 0 \\
Lat.\ media & 2\,715\,ms & 2\,711\,ms & $-$4\,ms \\
Lat.\ p90  & 3\,780\,ms & 3\,835\,ms & +55\,ms \\
Con fuentes & 100.0\,\% & 100.0\,\% & 0 \\
Rechazadas & 0.0\,\% & 0.0\,\% & 0 \\
\bottomrule
\end{tabular}
\end{table}

% ─── Tabla por categoría ────────────────────────────────────────────────────
\begin{table}[htbp]
\centering
\caption{Recall@5 y MRR por tipo de pregunta (ambas configuraciones)}
\label{tab:eval_categoria}
\begin{tabular}{lcrrrr}
\toprule
\textbf{Tipo} & \textbf{N}
  & \multicolumn{2}{c}{\textbf{LLM remoto}}
  & \multicolumn{2}{c}{\textbf{LLM local}} \\
\cmidrule(lr){3-4}\cmidrule(lr){5-6}
  & & R@5 & MRR & R@5 & MRR \\
\midrule
Recuperación directa & 15 & 1.0000 & 0.8167 & 1.0000 & 0.8167 \\
Síntesis             & 10 & 1.0000 & 0.8250 & 1.0000 & 0.8250 \\
Multifuente          & 10 & 1.0000 & 0.9500 & 1.0000 & 0.9500 \\
Semántica            &  8 & 1.0000 & 0.7396 & 1.0000 & 0.7396 \\
Razonamiento         &  7 & 1.0000 & 0.7190 & 1.0000 & 0.7190 \\
\bottomrule
\end{tabular}
\end{table}

% ─── Tabla por dificultad ───────────────────────────────────────────────────
\begin{table}[htbp]
\centering
\caption{Recall@5 y MRR por nivel de dificultad (ambas configuraciones)}
\label{tab:eval_dificultad}
\begin{tabular}{lcrrrr}
\toprule
\textbf{Nivel} & \textbf{N}
  & \multicolumn{2}{c}{\textbf{LLM remoto}}
  & \multicolumn{2}{c}{\textbf{LLM local}} \\
\cmidrule(lr){3-4}\cmidrule(lr){5-6}
  & & R@5 & MRR & R@5 & MRR \\
\midrule
Fácil (Nivel 1)        & 15 & 1.0000 & 0.8167 & 1.0000 & 0.8167 \\
Medio (Nivel 2)        & 10 & 1.0000 & 0.8250 & 1.0000 & 0.8250 \\
Difícil (Niveles 3--4) & 18 & 1.0000 & 0.8565 & 1.0000 & 0.8565 \\
Experto (Nivel 5)      &  7 & 1.0000 & 0.7190 & 1.0000 & 0.7190 \\
\bottomrule
\end{tabular}
\end{table}
```

---

## Conclusión para el TFG

El pipeline de recuperación vectorial es **robusto e independiente del LLM** empleado:
ambas configuraciones alcanzan **Recall@5 = 100 %** y **MRR = 0.82** sobre las 50 preguntas,
con latencias prácticamente idénticas (~2.7 s media).

Esto demuestra que la calidad de la recuperación reside en el índice Qdrant y el modelo de
embeddings (`intfloat/multilingual-e5-small`), lo que permite desplegar el sistema tanto en
entorno local como conectado a modelos de mayor capacidad sin sacrificar la cobertura documental.

_Generado: 2026-06-08_
