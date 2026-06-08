# Informe de Evaluación — RAG Canarias (LOCAL)

**Generado:** 2026-06-08T21:41:00Z  
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
| Latencia media | 26283 ms |
| Latencia mínima | 12756 ms |
| Latencia máxima | 78358 ms |
| Latencia p90 | 36179 ms |
| Respuestas con fuentes | 100.0% |
| Respuestas sin fuentes | 0.0% |
| Respuestas rechazadas | 0.0% |

---

## Rendimiento por categoría

| Categoría | N | Recall@5 | MRR | Lat. media |
|-----------|---|---------|-----|-----------|
| retrieval | 15 | 100.0% | 0.8167 | 26002 ms |
| synthesis | 10 | 100.0% | 0.8250 | 29237 ms |
| multisource | 10 | 100.0% | 0.9500 | 25412 ms |
| semantic | 8 | 100.0% | 0.7396 | 23376 ms |
| reasoning | 7 | 100.0% | 0.7190 | 27237 ms |

---

## Rendimiento por dificultad

| Dificultad | N | Recall@5 | MRR | Lat. media |
|------------|---|---------|-----|-----------|
| easy | 15 | 100.0% | 0.8167 | 26002 ms |
| medium | 10 | 100.0% | 0.8250 | 29237 ms |
| hard | 18 | 100.0% | 0.8565 | 24507 ms |
| expert | 7 | 100.0% | 0.7190 | 27237 ms |

---

## Casos destacados

**Mejor recuperación (MRR = 1.0):** preguntas [1, 2, 3, 6, 7, 8, 9, 10, 11, 14]
**Respuestas multifuente exitosas:** preguntas [4, 5, 12, 13, 14, 15, 17, 24, 25, 27]

---

## Resultados completos

| ID | Dif. | Cat. | R@1 | R@3 | R@5 | MRR | Lat. (ms) | Fuentes |
|----|------|------|-----|-----|-----|-----|-----------|---------|
| Q01 | easy | retrieval | 1 | 1 | 1 | 1.00 | 78358 | elmuseocanario.com |
| Q02 | easy | retrieval | 1 | 1 | 1 | 1.00 | 12756 | elmuseocanario.com |
| Q03 | easy | retrieval | 1 | 1 | 1 | 1.00 | 21396 | elmuseocanario.com |
| Q04 | easy | retrieval | 0 | 1 | 1 | 0.33 | 19825 | cultura.grancanaria.com, elmuseocanario.com |
| Q05 | easy | retrieval | 0 | 1 | 1 | 0.33 | 20120 | cultura.grancanaria.com, canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q06 | easy | retrieval | 1 | 1 | 1 | 1.00 | 35466 | memoriadelanzarote.com |
| Q07 | easy | retrieval | 1 | 1 | 1 | 1.00 | 24826 | memoriadelanzarote.com |
| Q08 | easy | retrieval | 1 | 1 | 1 | 1.00 | 16249 | canarias-azul.iatext.ulpgc.es |
| Q09 | easy | retrieval | 1 | 1 | 1 | 1.00 | 22303 | izuran.blogspot.com |
| Q10 | easy | retrieval | 1 | 1 | 1 | 1.00 | 20036 | www.academiacanarialengua.org |
| Q11 | easy | retrieval | 1 | 1 | 1 | 1.00 | 30522 | www.academiacanarialengua.org |
| Q12 | easy | retrieval | 0 | 0 | 1 | 0.25 | 13198 | www.academiacanarialengua.org, izuran.blogspot.com |
| Q13 | easy | retrieval | 0 | 1 | 1 | 0.33 | 18110 | www.academiacanarialengua.org, izuran.blogspot.com, canarias-azul.iatext.ulpgc.es |
| Q14 | easy | retrieval | 1 | 1 | 1 | 1.00 | 30220 | canarias-azul.iatext.ulpgc.es, izuran.blogspot.com, elmuseocanario.com |
| Q15 | easy | retrieval | 1 | 1 | 1 | 1.00 | 26642 | canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q16 | medium | synthesis | 1 | 1 | 1 | 1.00 | 51192 | memoriadelanzarote.com |
| Q17 | medium | synthesis | 0 | 0 | 1 | 0.25 | 33519 | cultura.grancanaria.com, elmuseocanario.com |
| Q18 | medium | synthesis | 1 | 1 | 1 | 1.00 | 18724 | elmuseocanario.com |
| Q19 | medium | synthesis | 1 | 1 | 1 | 1.00 | 32515 | canarias-azul.iatext.ulpgc.es |
| Q20 | medium | synthesis | 1 | 1 | 1 | 1.00 | 15250 | izuran.blogspot.com |
| Q21 | medium | synthesis | 1 | 1 | 1 | 1.00 | 23386 | www.academiacanarialengua.org |
| Q22 | medium | synthesis | 1 | 1 | 1 | 1.00 | 35462 | memoriadelanzarote.com |
| Q23 | medium | synthesis | 1 | 1 | 1 | 1.00 | 30943 | elmuseocanario.com |
| Q24 | medium | synthesis | 0 | 1 | 1 | 0.50 | 15578 | cultura.grancanaria.com, canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q25 | medium | synthesis | 0 | 1 | 1 | 0.50 | 35797 | elmuseocanario.com, izuran.blogspot.com, canarias-azul.iatext.ulpgc.es |
| Q26 | hard | multisource | 1 | 1 | 1 | 1.00 | 35135 | memoriadelanzarote.com |
| Q27 | hard | multisource | 1 | 1 | 1 | 1.00 | 16712 | canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q28 | hard | multisource | 1 | 1 | 1 | 1.00 | 27679 | izuran.blogspot.com, elmuseocanario.com |
| Q29 | hard | multisource | 1 | 1 | 1 | 1.00 | 27684 | www.academiacanarialengua.org, canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q30 | hard | multisource | 1 | 1 | 1 | 1.00 | 15260 | canarias-azul.iatext.ulpgc.es |
| Q31 | hard | multisource | 1 | 1 | 1 | 1.00 | 30035 | memoriadelanzarote.com |
| Q32 | hard | multisource | 1 | 1 | 1 | 1.00 | 18038 | cultura.grancanaria.com, www.academiacanarialengua.org, canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q33 | hard | multisource | 1 | 1 | 1 | 1.00 | 27554 | cultura.grancanaria.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q34 | hard | multisource | 0 | 1 | 1 | 0.50 | 27275 | cultura.grancanaria.com, elmuseocanario.com |
| Q35 | hard | multisource | 1 | 1 | 1 | 1.00 | 28745 | www.academiacanarialengua.org, izuran.blogspot.com, elmuseocanario.com |
| Q36 | hard | semantic | 1 | 1 | 1 | 1.00 | 19876 | cultura.grancanaria.com, canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q37 | hard | semantic | 1 | 1 | 1 | 1.00 | 24503 | cultura.grancanaria.com, canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q38 | hard | semantic | 1 | 1 | 1 | 1.00 | 36184 | canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q39 | hard | semantic | 0 | 1 | 1 | 0.33 | 25237 | elmuseocanario.com, www.academiacanarialengua.org, canarias-azul.iatext.ulpgc.es |
| Q40 | hard | semantic | 1 | 1 | 1 | 1.00 | 22731 | www.academiacanarialengua.org |
| Q41 | hard | semantic | 1 | 1 | 1 | 1.00 | 15711 | cultura.grancanaria.com, canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q42 | hard | semantic | 0 | 0 | 1 | 0.25 | 27303 | elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q43 | hard | semantic | 0 | 1 | 1 | 0.33 | 15459 | cultura.grancanaria.com, elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q44 | expert | reasoning | 1 | 1 | 1 | 1.00 | 13836 | cultura.grancanaria.com, elmuseocanario.com |
| Q45 | expert | reasoning | 1 | 1 | 1 | 1.00 | 36179 | izuran.blogspot.com, elmuseocanario.com |
| Q46 | expert | reasoning | 0 | 1 | 1 | 0.50 | 27582 | cultura.grancanaria.com, canarias-azul.iatext.ulpgc.es, elmuseocanario.com |
| Q47 | expert | reasoning | 0 | 0 | 1 | 0.20 | 21382 | cultura.grancanaria.com, www.academiacanarialengua.org, canarias-azul.iatext.ulpgc.es |
| Q48 | expert | reasoning | 1 | 1 | 1 | 1.00 | 24350 | cultura.grancanaria.com, elmuseocanario.com |
| Q49 | expert | reasoning | 0 | 1 | 1 | 0.33 | 31142 | elmuseocanario.com, canarias-azul.iatext.ulpgc.es |
| Q50 | expert | reasoning | 1 | 1 | 1 | 1.00 | 36184 | cultura.grancanaria.com, canarias-azul.iatext.ulpgc.es, elmuseocanario.com |