# Informe de Evaluación — RAG Canarias (LOCAL)

**Generado:** 2026-06-08T21:30:29Z  
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
| Latencia media | 2651 ms |
| Latencia mínima | 1044 ms |
| Latencia máxima | 7189 ms |
| Latencia p90 | 3622 ms |
| Respuestas con fuentes | 100.0% |
| Respuestas sin fuentes | 0.0% |
| Respuestas rechazadas | 0.0% |

---

## Rendimiento por categoría

| Categoría | N | Recall@5 | MRR | Lat. media |
|-----------|---|---------|-----|-----------|
| retrieval | 15 | 100.0% | 0.8167 | 2586 ms |
| synthesis | 10 | 100.0% | 0.8250 | 2748 ms |
| multisource | 10 | 100.0% | 0.9500 | 2687 ms |
| semantic | 8 | 100.0% | 0.7396 | 2436 ms |
| reasoning | 7 | 100.0% | 0.7190 | 2847 ms |

---

## Rendimiento por dificultad

| Dificultad | N | Recall@5 | MRR | Lat. media |
|------------|---|---------|-----|-----------|
| easy | 15 | 100.0% | 0.8167 | 2586 ms |
| medium | 10 | 100.0% | 0.8250 | 2748 ms |
| hard | 18 | 100.0% | 0.8565 | 2576 ms |
| expert | 7 | 100.0% | 0.7190 | 2847 ms |

---

## Casos destacados

**Mejor recuperación (MRR = 1.0):** preguntas [1, 2, 3, 6, 7, 8, 9, 10, 11, 14]
**Respuestas multifuente exitosas:** preguntas [4, 5, 12, 13, 14, 15, 17, 24, 25, 27]

---

## Resultados completos

| ID | Dif. | Cat. | R@1 | R@3 | R@5 | MRR | Lat. (ms) | Fuentes |
|----|------|------|-----|-----|-----|-----|-----------|---------|
| Q01 | easy | retrieval | 1 | 1 | 1 | 1.00 | 7189 | elmuseocanario.com |
| Q02 | easy | retrieval | 1 | 1 | 1 | 1.00 | 1044 | elmuseocanario.com |
| Q03 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2007 | elmuseocanario.com |
| Q04 | easy | retrieval | 0 | 1 | 1 | 0.33 | 1900 | cultura.grancanaria.com, elmuseocanario.com |
| Q05 | easy | retrieval | 0 | 1 | 1 | 0.33 | 2586 | cultura.grancanaria.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q06 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2627 | memoriadelanzarote.com |
| Q07 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2421 | memoriadelanzarote.com |
| Q08 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2258 | canarias-azul.iatext.ulpgc.es |
| Q09 | easy | retrieval | 1 | 1 | 1 | 1.00 | 3837 | izuran.blogspot.com |
| Q10 | easy | retrieval | 1 | 1 | 1 | 1.00 | 1911 | www.academiacanarialengua.org |
| Q11 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2473 | www.academiacanarialengua.org |
| Q12 | easy | retrieval | 0 | 0 | 1 | 0.25 | 1214 | www.academiacanarialengua.org, izuran.blogspot.com |
| Q13 | easy | retrieval | 0 | 1 | 1 | 0.33 | 1736 | www.academiacanarialengua.org, izuran.blogspot.com, canarias-azul.iatext.ulpgc.es |
| Q14 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2673 | elmuseocanario.com, izuran.blogspot.com, canarias-azul.iatext.ulpgc.es |
| Q15 | easy | retrieval | 1 | 1 | 1 | 1.00 | 2908 | elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q16 | medium | synthesis | 1 | 1 | 1 | 1.00 | 2874 | memoriadelanzarote.com |
| Q17 | medium | synthesis | 0 | 0 | 1 | 0.25 | 2822 | cultura.grancanaria.com, elmuseocanario.com |
| Q18 | medium | synthesis | 1 | 1 | 1 | 1.00 | 2675 | elmuseocanario.com |
| Q19 | medium | synthesis | 1 | 1 | 1 | 1.00 | 2961 | canarias-azul.iatext.ulpgc.es |
| Q20 | medium | synthesis | 1 | 1 | 1 | 1.00 | 3233 | izuran.blogspot.com |
| Q21 | medium | synthesis | 1 | 1 | 1 | 1.00 | 2014 | www.academiacanarialengua.org |
| Q22 | medium | synthesis | 1 | 1 | 1 | 1.00 | 3156 | memoriadelanzarote.com |
| Q23 | medium | synthesis | 1 | 1 | 1 | 1.00 | 3709 | elmuseocanario.com |
| Q24 | medium | synthesis | 0 | 1 | 1 | 0.50 | 1701 | cultura.grancanaria.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q25 | medium | synthesis | 0 | 1 | 1 | 0.50 | 2331 | elmuseocanario.com, izuran.blogspot.com, canarias-azul.iatext.ulpgc.es |
| Q26 | hard | multisource | 1 | 1 | 1 | 1.00 | 3622 | memoriadelanzarote.com |
| Q27 | hard | multisource | 1 | 1 | 1 | 1.00 | 2382 | elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q28 | hard | multisource | 1 | 1 | 1 | 1.00 | 2365 | elmuseocanario.com, izuran.blogspot.com |
| Q29 | hard | multisource | 1 | 1 | 1 | 1.00 | 2391 | elmuseocanario.com, www.academiacanarialengua.org, canarias-azul.iatext.ulpgc.es |
| Q30 | hard | multisource | 1 | 1 | 1 | 1.00 | 1757 | canarias-azul.iatext.ulpgc.es |
| Q31 | hard | multisource | 1 | 1 | 1 | 1.00 | 3033 | memoriadelanzarote.com |
| Q32 | hard | multisource | 1 | 1 | 1 | 1.00 | 2431 | cultura.grancanaria.com, elmuseocanario.com, www.academiacanarialengua.org, canarias-azul.iatext.ulpgc.es |
| Q33 | hard | multisource | 1 | 1 | 1 | 1.00 | 3632 | cultura.grancanaria.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q34 | hard | multisource | 0 | 1 | 1 | 0.50 | 2830 | cultura.grancanaria.com, elmuseocanario.com |
| Q35 | hard | multisource | 1 | 1 | 1 | 1.00 | 2431 | elmuseocanario.com, www.academiacanarialengua.org, izuran.blogspot.com |
| Q36 | hard | semantic | 1 | 1 | 1 | 1.00 | 1885 | cultura.grancanaria.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q37 | hard | semantic | 1 | 1 | 1 | 1.00 | 3178 | cultura.grancanaria.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q38 | hard | semantic | 1 | 1 | 1 | 1.00 | 2639 | elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q39 | hard | semantic | 0 | 1 | 1 | 0.33 | 1653 | elmuseocanario.com, www.academiacanarialengua.org, canarias-azul.iatext.ulpgc.es |
| Q40 | hard | semantic | 1 | 1 | 1 | 1.00 | 2792 | www.academiacanarialengua.org |
| Q41 | hard | semantic | 1 | 1 | 1 | 1.00 | 2954 | cultura.grancanaria.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q42 | hard | semantic | 0 | 0 | 1 | 0.25 | 2082 | elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q43 | hard | semantic | 0 | 1 | 1 | 0.33 | 2304 | cultura.grancanaria.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q44 | expert | reasoning | 1 | 1 | 1 | 1.00 | 3121 | cultura.grancanaria.com, elmuseocanario.com |
| Q45 | expert | reasoning | 1 | 1 | 1 | 1.00 | 2964 | elmuseocanario.com, izuran.blogspot.com |
| Q46 | expert | reasoning | 0 | 1 | 1 | 0.50 | 2368 | cultura.grancanaria.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q47 | expert | reasoning | 0 | 0 | 1 | 0.20 | 3005 | cultura.grancanaria.com, www.academiacanarialengua.org, canarias-azul.iatext.ulpgc.es |
| Q48 | expert | reasoning | 1 | 1 | 1 | 1.00 | 2810 | cultura.grancanaria.com, elmuseocanario.com |
| Q49 | expert | reasoning | 0 | 1 | 1 | 0.33 | 3128 | elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q50 | expert | reasoning | 1 | 1 | 1 | 1.00 | 2530 | cultura.grancanaria.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |