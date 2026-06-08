% Tablas de evaluación — RAG Canarias
% Generado: 2026-06-08T21:30:29Z
% Copiar directamente en el capítulo de evaluación del TFG.
% Requiere \usepackage{booktabs} en el preámbulo.

% ─────────────────────────────────────────────────────────────────────
% TABLA 1: Métricas globales
% ─────────────────────────────────────────────────────────────────────

\begin{table}[htbp]
\centering
\caption{Métricas globales de evaluación del sistema RAG Canarias}
\label{tab:eval_global}
\begin{tabular}{lc}
\toprule
\textbf{Métrica} & \textbf{Valor} \\
\midrule
Recall@1  & 0.7200 \\
Recall@3  & 0.9200 \\
Recall@5  & 1.0000 \\
MRR       & 0.8190 \\
\midrule
Latencia media (ms) & 2651 \\
Latencia p90 (ms)   & 3622 \\
\midrule
Respuestas con fuentes (\%) & 100.0 \\
Respuestas rechazadas (\%)  & 0.0 \\
\bottomrule
\end{tabular}
\end{table}

% ─────────────────────────────────────────────────────────────────────
% TABLA 2: Métricas por tipo de pregunta (categoría)
% ─────────────────────────────────────────────────────────────────────

\begin{table}[htbp]
\centering
\caption{Recall@5 y latencia media por tipo de pregunta}
\label{tab:eval_categoria}
\begin{tabular}{lcccc}
\toprule
\textbf{Tipo} & \textbf{N} & \textbf{Recall@5} & \textbf{MRR} & \textbf{Lat. media (ms)} \\
\midrule
Recuperación directa & 15 & 1.0000 & 0.8167 & 2586 \\
Síntesis & 10 & 1.0000 & 0.8250 & 2748 \\
Multifuente & 10 & 1.0000 & 0.9500 & 2687 \\
Semántica & 8 & 1.0000 & 0.7396 & 2436 \\
Razonamiento & 7 & 1.0000 & 0.7190 & 2847 \\
\bottomrule
\end{tabular}
\end{table}

% ─────────────────────────────────────────────────────────────────────
% TABLA 3: Métricas por nivel de dificultad
% ─────────────────────────────────────────────────────────────────────

\begin{table}[htbp]
\centering
\caption{Recall@5 y latencia media por nivel de dificultad}
\label{tab:eval_dificultad}
\begin{tabular}{lcccc}
\toprule
\textbf{Nivel} & \textbf{N} & \textbf{Recall@5} & \textbf{MRR} & \textbf{Lat. media (ms)} \\
\midrule
Fácil (Nivel 1) & 15 & 1.0000 & 0.8167 & 2586 \\
Medio (Nivel 2) & 10 & 1.0000 & 0.8250 & 2748 \\
Difícil (Niveles 3--4) & 18 & 1.0000 & 0.8565 & 2576 \\
Experto (Nivel 5) & 7 & 1.0000 & 0.7190 & 2847 \\
\bottomrule
\end{tabular}
\end{table}

% Fin de tablas de evaluación