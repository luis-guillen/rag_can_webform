% Tablas de evaluación — RAG Canarias
% Generado: 2026-06-08T20:34:58Z
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
Latencia media (ms) & 2711 \\
Latencia p90 (ms)   & 3835 \\
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
Recuperación directa & 15 & 1.0000 & 0.8167 & 2602 \\
Síntesis & 10 & 1.0000 & 0.8250 & 2886 \\
Multifuente & 10 & 1.0000 & 0.9500 & 2810 \\
Semántica & 8 & 1.0000 & 0.7396 & 2429 \\
Razonamiento & 7 & 1.0000 & 0.7190 & 2873 \\
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
Fácil (Nivel 1) & 15 & 1.0000 & 0.8167 & 2602 \\
Medio (Nivel 2) & 10 & 1.0000 & 0.8250 & 2886 \\
Difícil (Niveles 3--4) & 18 & 1.0000 & 0.8565 & 2640 \\
Experto (Nivel 5) & 7 & 1.0000 & 0.7190 & 2873 \\
\bottomrule
\end{tabular}
\end{table}

% Fin de tablas de evaluación