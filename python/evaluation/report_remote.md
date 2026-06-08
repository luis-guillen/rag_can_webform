# Informe de Evaluación — RAG Canarias

**Generado:** 2026-06-08T20:09:24Z  
**API:** http://127.0.0.1:8000/query  
**Top-K:** 5

---

## Resumen ejecutivo

| Métrica | Valor |
|---------|-------|
| Total preguntas | 50 |
| Preguntas con score (expected_sources ≠ ∅) | 50 |
| Errores de API | 0 |
| Recall@1 | 72.0% |
| Recall@3 | 92.0% |
| Recall@5 | 100.0% |
| MRR | 0.8190 |
| Latencia media | 2715 ms |
| Latencia mínima | 919 ms |
| Latencia máxima | 7327 ms |
| Latencia p90 | 3780 ms |
| Respuestas con fuentes | 100.0% |
| Respuestas sin fuentes | 0.0% |
| Respuestas rechazadas | 0.0% |

---

## Rendimiento por categoría

| Categoría | N | Recall@5 | MRR | Lat. media |
|-----------|---|---------|-----|-----------|
| retrieval | 15 | 100.0% | 0.8167 | 2546 ms |
| synthesis | 10 | 100.0% | 0.8250 | 2723 ms |
| multisource | 10 | 100.0% | 0.9500 | 2707 ms |
| semantic | 8 | 100.0% | 0.7396 | 3111 ms |
| reasoning | 7 | 100.0% | 0.7190 | 2627 ms |

---

## Rendimiento por dificultad

| Dificultad | N | Recall@5 | MRR | Lat. media |
|------------|---|---------|-----|-----------|
| easy | 15 | 100.0% | 0.8167 | 2546 ms |
| medium | 10 | 100.0% | 0.8250 | 2723 ms |
| hard | 18 | 100.0% | 0.8565 | 2886 ms |
| expert | 7 | 100.0% | 0.7190 | 2627 ms |

---

## Casos destacados

**Mejor recuperación (MRR = 1.0):** preguntas [1, 2, 3, 6, 7, 8, 9, 10, 11, 14]
**Respuestas multifuente exitosas:** preguntas [4, 5, 12, 13, 14, 15, 17, 24, 25, 27]

---

## Resultados completos

| ID | Dif. | Cat. | R@1 | R@3 | R@5 | MRR | Lat. (ms) | Fuentes |
|----|------|------|-----|-----|-----|-----|-----------|---------|
| Q01 | easy | retrieval | 1 | 1 | 1 | 1.00 | 7327 | elmuseocanario.com |
| Q02 | easy | retrieval | 1 | 1 | 1 | 1.00 | 919 | elmuseocanario.com |
| Q03 | easy | retrieval | 1 | 1 | 1 | 1.00 | 1769 | elmuseocanario.com |
| Q04 | easy | retrieval | 0 | 1 | 1 | 0.33 | 1839 | cultura.grancanaria.com, elmuseocanario.com |
| Q05 | easy | retrieval | 0 | 1 | 1 | 0.33 | 2472 | cultura.grancanaria.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q06 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2278 | memoriadelanzarote.com |
| Q07 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2186 | memoriadelanzarote.com |
| Q08 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2130 | canarias-azul.iatext.ulpgc.es |
| Q09 | easy | retrieval | 1 | 1 | 1 | 1.00 | 3444 | izuran.blogspot.com |
| Q10 | easy | retrieval | 1 | 1 | 1 | 1.00 | 1841 | www.academiacanarialengua.org |
| Q11 | easy | retrieval | 1 | 1 | 1 | 1.00 | 4182 | www.academiacanarialengua.org |
| Q12 | easy | retrieval | 0 | 0 | 1 | 0.25 | 1118 | izuran.blogspot.com, www.academiacanarialengua.org |
| Q13 | easy | retrieval | 0 | 1 | 1 | 0.33 | 1800 | izuran.blogspot.com, canarias-azul.iatext.ulpgc.es, www.academiacanarialengua.org |
| Q14 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2204 | izuran.blogspot.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q15 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2689 | elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q16 | medium | synthesis | 1 | 1 | 1 | 1.00 | 2866 | memoriadelanzarote.com |
| Q17 | medium | synthesis | 0 | 0 | 1 | 0.25 | 2790 | cultura.grancanaria.com, elmuseocanario.com |
| Q18 | medium | synthesis | 1 | 1 | 1 | 1.00 | 2474 | elmuseocanario.com |
| Q19 | medium | synthesis | 1 | 1 | 1 | 1.00 | 2860 | canarias-azul.iatext.ulpgc.es |
| Q20 | medium | synthesis | 1 | 1 | 1 | 1.00 | 3495 | izuran.blogspot.com |
| Q21 | medium | synthesis | 1 | 1 | 1 | 1.00 | 1787 | www.academiacanarialengua.org |
| Q22 | medium | synthesis | 1 | 1 | 1 | 1.00 | 3495 | memoriadelanzarote.com |
| Q23 | medium | synthesis | 1 | 1 | 1 | 1.00 | 3591 | elmuseocanario.com |
| Q24 | medium | synthesis | 0 | 1 | 1 | 0.50 | 1658 | elmuseocanario.com, cultura.grancanaria.com, canarias-azul.iatext.ulpgc.es |
| Q25 | medium | synthesis | 0 | 1 | 1 | 0.50 | 2212 | izuran.blogspot.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q26 | hard | multisource | 1 | 1 | 1 | 1.00 | 3489 | memoriadelanzarote.com |
| Q27 | hard | multisource | 1 | 1 | 1 | 1.00 | 2291 | elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q28 | hard | multisource | 1 | 1 | 1 | 1.00 | 1916 | izuran.blogspot.com, elmuseocanario.com |
| Q29 | hard | multisource | 1 | 1 | 1 | 1.00 | 2261 | elmuseocanario.com, www.academiacanarialengua.org, canarias-azul.iatext.ulpgc.es |
| Q30 | hard | multisource | 1 | 1 | 1 | 1.00 | 1668 | canarias-azul.iatext.ulpgc.es |
| Q31 | hard | multisource | 1 | 1 | 1 | 1.00 | 3213 | memoriadelanzarote.com |
| Q32 | hard | multisource | 1 | 1 | 1 | 1.00 | 2415 | cultura.grancanaria.com, elmuseocanario.com, www.academiacanarialengua.org, canarias-azul.iatext.ulpgc.es |
| Q33 | hard | multisource | 1 | 1 | 1 | 1.00 | 3433 | elmuseocanario.com, cultura.grancanaria.com, canarias-azul.iatext.ulpgc.es |
| Q34 | hard | multisource | 0 | 1 | 1 | 0.50 | 2599 | elmuseocanario.com, cultura.grancanaria.com |
| Q35 | hard | multisource | 1 | 1 | 1 | 1.00 | 3780 | izuran.blogspot.com, elmuseocanario.com, www.academiacanarialengua.org |
| Q36 | hard | semantic | 1 | 1 | 1 | 1.00 | 1719 | cultura.grancanaria.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q37 | hard | semantic | 1 | 1 | 1 | 1.00 | 4829 | cultura.grancanaria.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q38 | hard | semantic | 1 | 1 | 1 | 1.00 | 2425 | elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q39 | hard | semantic | 0 | 1 | 1 | 0.33 | 2108 | elmuseocanario.com, canarias-azul.iatext.ulpgc.es, www.academiacanarialengua.org |
| Q40 | hard | semantic | 1 | 1 | 1 | 1.00 | 2689 | www.academiacanarialengua.org |
| Q41 | hard | semantic | 1 | 1 | 1 | 1.00 | 6840 | cultura.grancanaria.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q42 | hard | semantic | 0 | 0 | 1 | 0.25 | 2019 | elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q43 | hard | semantic | 0 | 1 | 1 | 0.33 | 2258 | cultura.grancanaria.com, canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q44 | expert | reasoning | 1 | 1 | 1 | 1.00 | 2706 | cultura.grancanaria.com, elmuseocanario.com |
| Q45 | expert | reasoning | 1 | 1 | 1 | 1.00 | 2647 | izuran.blogspot.com, elmuseocanario.com |
| Q46 | expert | reasoning | 0 | 1 | 1 | 0.50 | 2232 | elmuseocanario.com, cultura.grancanaria.com, canarias-azul.iatext.ulpgc.es |
| Q47 | expert | reasoning | 0 | 0 | 1 | 0.20 | 2946 | cultura.grancanaria.com, canarias-azul.iatext.ulpgc.es, www.academiacanarialengua.org |
| Q48 | expert | reasoning | 1 | 1 | 1 | 1.00 | 2491 | cultura.grancanaria.com, elmuseocanario.com |
| Q49 | expert | reasoning | 0 | 1 | 1 | 0.33 | 2901 | elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q50 | expert | reasoning | 1 | 1 | 1 | 1.00 | 2468 | cultura.grancanaria.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |