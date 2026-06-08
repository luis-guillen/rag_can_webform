# Informe de Evaluación — RAG Canarias (LOCAL)

**Generado:** 2026-06-08T20:34:58Z  
**Configuración LLM:** local  
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
| Latencia media | 2711 ms |
| Latencia mínima | 967 ms |
| Latencia máxima | 7159 ms |
| Latencia p90 | 3835 ms |
| Respuestas con fuentes | 100.0% |
| Respuestas sin fuentes | 0.0% |
| Respuestas rechazadas | 0.0% |

---

## Rendimiento por categoría

| Categoría | N | Recall@5 | MRR | Lat. media |
|-----------|---|---------|-----|-----------|
| retrieval | 15 | 100.0% | 0.8167 | 2602 ms |
| synthesis | 10 | 100.0% | 0.8250 | 2886 ms |
| multisource | 10 | 100.0% | 0.9500 | 2810 ms |
| semantic | 8 | 100.0% | 0.7396 | 2429 ms |
| reasoning | 7 | 100.0% | 0.7190 | 2873 ms |

---

## Rendimiento por dificultad

| Dificultad | N | Recall@5 | MRR | Lat. media |
|------------|---|---------|-----|-----------|
| easy | 15 | 100.0% | 0.8167 | 2602 ms |
| medium | 10 | 100.0% | 0.8250 | 2886 ms |
| hard | 18 | 100.0% | 0.8565 | 2640 ms |
| expert | 7 | 100.0% | 0.7190 | 2873 ms |

---

## Casos destacados

**Mejor recuperación (MRR = 1.0):** preguntas [1, 2, 3, 6, 7, 8, 9, 10, 11, 14]
**Respuestas multifuente exitosas:** preguntas [4, 5, 12, 13, 14, 15, 17, 24, 25, 27]

---

## Resultados completos

| ID | Dif. | Cat. | R@1 | R@3 | R@5 | MRR | Lat. (ms) | Fuentes |
|----|------|------|-----|-----|-----|-----|-----------|---------|
| Q01 | easy | retrieval | 1 | 1 | 1 | 1.00 | 7159 | elmuseocanario.com |
| Q02 | easy | retrieval | 1 | 1 | 1 | 1.00 | 967 | elmuseocanario.com |
| Q03 | easy | retrieval | 1 | 1 | 1 | 1.00 | 1849 | elmuseocanario.com |
| Q04 | easy | retrieval | 0 | 1 | 1 | 0.33 | 1772 | cultura.grancanaria.com, elmuseocanario.com |
| Q05 | easy | retrieval | 0 | 1 | 1 | 0.33 | 2571 | canarias-azul.iatext.ulpgc.es, cultura.grancanaria.com, elmuseocanario.com |
| Q06 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2651 | memoriadelanzarote.com |
| Q07 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2423 | memoriadelanzarote.com |
| Q08 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2451 | canarias-azul.iatext.ulpgc.es |
| Q09 | easy | retrieval | 1 | 1 | 1 | 1.00 | 3913 | izuran.blogspot.com |
| Q10 | easy | retrieval | 1 | 1 | 1 | 1.00 | 1836 | www.academiacanarialengua.org |
| Q11 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2678 | www.academiacanarialengua.org |
| Q12 | easy | retrieval | 0 | 0 | 1 | 0.25 | 1231 | www.academiacanarialengua.org, izuran.blogspot.com |
| Q13 | easy | retrieval | 0 | 1 | 1 | 0.33 | 1866 | canarias-azul.iatext.ulpgc.es, www.academiacanarialengua.org, izuran.blogspot.com |
| Q14 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2865 | izuran.blogspot.com, canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q15 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2799 | canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q16 | medium | synthesis | 1 | 1 | 1 | 1.00 | 2586 | memoriadelanzarote.com |
| Q17 | medium | synthesis | 0 | 0 | 1 | 0.25 | 2909 | cultura.grancanaria.com, elmuseocanario.com |
| Q18 | medium | synthesis | 1 | 1 | 1 | 1.00 | 2649 | elmuseocanario.com |
| Q19 | medium | synthesis | 1 | 1 | 1 | 1.00 | 3260 | canarias-azul.iatext.ulpgc.es |
| Q20 | medium | synthesis | 1 | 1 | 1 | 1.00 | 3290 | izuran.blogspot.com |
| Q21 | medium | synthesis | 1 | 1 | 1 | 1.00 | 1992 | www.academiacanarialengua.org |
| Q22 | medium | synthesis | 1 | 1 | 1 | 1.00 | 4361 | memoriadelanzarote.com |
| Q23 | medium | synthesis | 1 | 1 | 1 | 1.00 | 3761 | elmuseocanario.com |
| Q24 | medium | synthesis | 0 | 1 | 1 | 0.50 | 1738 | canarias-azul.iatext.ulpgc.es, cultura.grancanaria.com, elmuseocanario.com |
| Q25 | medium | synthesis | 0 | 1 | 1 | 0.50 | 2315 | izuran.blogspot.com, canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q26 | hard | multisource | 1 | 1 | 1 | 1.00 | 3678 | memoriadelanzarote.com |
| Q27 | hard | multisource | 1 | 1 | 1 | 1.00 | 2326 | canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q28 | hard | multisource | 1 | 1 | 1 | 1.00 | 2481 | izuran.blogspot.com, elmuseocanario.com |
| Q29 | hard | multisource | 1 | 1 | 1 | 1.00 | 2413 | canarias-azul.iatext.ulpgc.es, www.academiacanarialengua.org, elmuseocanario.com |
| Q30 | hard | multisource | 1 | 1 | 1 | 1.00 | 1887 | canarias-azul.iatext.ulpgc.es |
| Q31 | hard | multisource | 1 | 1 | 1 | 1.00 | 3194 | memoriadelanzarote.com |
| Q32 | hard | multisource | 1 | 1 | 1 | 1.00 | 2736 | canarias-azul.iatext.ulpgc.es, cultura.grancanaria.com, www.academiacanarialengua.org, elmuseocanario.com |
| Q33 | hard | multisource | 1 | 1 | 1 | 1.00 | 4039 | canarias-azul.iatext.ulpgc.es, cultura.grancanaria.com, elmuseocanario.com |
| Q34 | hard | multisource | 0 | 1 | 1 | 0.50 | 2935 | cultura.grancanaria.com, elmuseocanario.com |
| Q35 | hard | multisource | 1 | 1 | 1 | 1.00 | 2406 | elmuseocanario.com, www.academiacanarialengua.org, izuran.blogspot.com |
| Q36 | hard | semantic | 1 | 1 | 1 | 1.00 | 1717 | canarias-azul.iatext.ulpgc.es, cultura.grancanaria.com, elmuseocanario.com |
| Q37 | hard | semantic | 1 | 1 | 1 | 1.00 | 3835 | canarias-azul.iatext.ulpgc.es, cultura.grancanaria.com, elmuseocanario.com |
| Q38 | hard | semantic | 1 | 1 | 1 | 1.00 | 2632 | canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q39 | hard | semantic | 0 | 1 | 1 | 0.33 | 1567 | canarias-azul.iatext.ulpgc.es, www.academiacanarialengua.org, elmuseocanario.com |
| Q40 | hard | semantic | 1 | 1 | 1 | 1.00 | 2468 | www.academiacanarialengua.org |
| Q41 | hard | semantic | 1 | 1 | 1 | 1.00 | 2831 | cultura.grancanaria.com, canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q42 | hard | semantic | 0 | 0 | 1 | 0.25 | 2113 | canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q43 | hard | semantic | 0 | 1 | 1 | 0.33 | 2270 | cultura.grancanaria.com, canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q44 | expert | reasoning | 1 | 1 | 1 | 1.00 | 3438 | cultura.grancanaria.com, elmuseocanario.com |
| Q45 | expert | reasoning | 1 | 1 | 1 | 1.00 | 3073 | izuran.blogspot.com, elmuseocanario.com |
| Q46 | expert | reasoning | 0 | 1 | 1 | 0.50 | 2260 | canarias-azul.iatext.ulpgc.es, cultura.grancanaria.com, elmuseocanario.com |
| Q47 | expert | reasoning | 0 | 0 | 1 | 0.20 | 2933 | cultura.grancanaria.com, canarias-azul.iatext.ulpgc.es, www.academiacanarialengua.org |
| Q48 | expert | reasoning | 1 | 1 | 1 | 1.00 | 2629 | cultura.grancanaria.com, elmuseocanario.com |
| Q49 | expert | reasoning | 0 | 1 | 1 | 0.33 | 3105 | canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q50 | expert | reasoning | 1 | 1 | 1 | 1.00 | 2674 | canarias-azul.iatext.ulpgc.es, cultura.grancanaria.com, elmuseocanario.com |