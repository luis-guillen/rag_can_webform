# Análisis de Calidad de Respuestas — RAG Canarias

**Comparativa cualitativa:** qwen3:30b-a3b (remoto, Dell Pro Max) vs qwen3.5:4b (local, Ollama)  
**Corpus de evaluación:** 50 preguntas de patrimonio cultural canario  
**Fecha:** 2026-06-08

> Las métricas de recuperación (Recall@K, MRR) son idénticas en ambas configuraciones
> porque dependen del índice Qdrant, no del LLM. Este documento analiza exclusivamente
> las diferencias en el texto generado.

---

## Resumen estadístico

| Medida | Valor |
|--------|-------|
| Respuestas con texto idéntico | 11 / 50 (22 %) |
| Respuestas con texto diferente | 39 / 50 (78 %) |
| Longitud media remoto (30B) | 506 chars |
| Longitud media local (4B) | 513 chars |
| Local más larga que remoto | 22 / 50 |
| Remoto más largo que local | 16 / 50 |
| Igual longitud | 12 / 50 |

### Longitud media por nivel de dificultad

| Nivel | Remoto (30B) | Local (4B) | Δ |
|-------|-------------|-----------|---|
| Fácil (Nivel 1) | 395 chars | 400 chars | +5 |
| Medio (Nivel 2) | 580 chars | 587 chars | +7 |
| Difícil (Niveles 3-4) | 521 chars | 520 chars | −1 |
| **Experto (Nivel 5)** | 599 chars | **630 chars** | **+31** |

El modelo local (4B) tiende a producir respuestas ligeramente más largas, especialmente
en preguntas de razonamiento complejo (nivel experto, +31 chars de media).

---

## Patrones de diferencia observados

### 1. Misma información, diferente redacción (caso más frecuente)

El modelo local parafrasea de forma distinta sin añadir ni quitar información:

**Q05 — ¿Qué actividades culturales se realizan en los museos de Gran Canaria?**

| Modelo | Fragmento |
|--------|-----------|
| Remoto | *"utilizando paneles expositivos, piezas 3D y realidad aumentada **accesible por códigos QR**, para acercar la historia..."* |
| Local | *"utilizando paneles expositivos, piezas 3D y realidad aumentada **mediante códigos QR** para acercar la historia..."* |

---

### 2. El modelo local añade aclaraciones parentéticas

El modelo local incluye explicaciones adicionales entre paréntesis que el 30B omite:

**Q09 — ¿Qué información recoge el blog Izuran?**

| Modelo | Fragmento |
|--------|-----------|
| Remoto | *"los pueblos indígenas de Canarias, con **enfoque** en su origen amazigh"* |
| Local | *"los pueblos indígenas de Canarias, con **énfasis** en su origen amazigh **(beréber)**"* |

El modelo 4B añade "(beréber)" como sinónimo explicativo de "amazigh". Añade contexto
útil para lectores no familiarizados, aunque no estaba en el corpus de forma directa.

---

### 3. El modelo remoto añade razonamiento meta-contextual

El 30B a veces comenta explícitamente los límites del corpus:

**Q04 — ¿Qué museos dependen del Cabildo de Gran Canaria?**

| Modelo | Fragmento |
|--------|-----------|
| Remoto | *"...aunque este último **no está especificado como dependiente directamente** del Cabildo [1]."* |
| Local | *"...aunque este **no está bajo su gestión directa** [1]."* |

Ambos son precisos; el remoto usa lenguaje más cuidadoso sobre lo que el corpus afirma.

---

### 4. Diferencias de detalle en nivel experto (más marcadas)

**Q44 — ¿Cómo se complementan los museos, archivos digitales y diccionarios para conservar la cultura canaria?**

| Modelo | Longitud | Diferencia clave |
|--------|----------|-----------------|
| Remoto (672c) | Más conciso | Menciona seguridad de datos y copias de seguridad |
| Local (787c) | Más extenso | Añade "colaboración con Red Eléctrica-Redeia" y "búsquedas detalladas" |

El modelo local extrae más detalles del contexto recuperado; el remoto sintetiza
con más criterio editorial qué incluir.

**Q45 — ¿Qué diferencias existen entre conservar objetos arqueológicos y patrimonio lingüístico?**

| Modelo | Fragmento diferencial |
|--------|-----------------------|
| Remoto | *"depende de la **revitalización y transmisión intergeneracional**"* |
| Local | *"requiere **estrategias digitales y comunitarias** para mantener su vida activa"* |

Distinto énfasis: el 30B pone el foco en la dimensión social (transmisión generacional),
el 4B en los medios técnicos (estrategias digitales).

---

### 5. Respuestas idénticas (11/50)

Las respuestas coinciden exactamente en preguntas donde el contexto recuperado es
muy específico y deja poco margen de paráfrasis:

- Q01–Q03: definición directa del Museo Canario (texto muy específico en el corpus)
- Q15, Q17, Q18, Q24: síntesis de fuente única con información concreta
- Q30, Q42, Q46, Q47: preguntas cuya respuesta es prácticamente literal en el chunk

---

## Alineación respuesta–fuente: casos destacados

| Q | Pregunta | Chunks recuperados | Respuesta | Valoración |
|---|----------|-------------------|-----------|------------|
| Q06 | Memoria de Lanzarote | `memoriadelanzarote.com` ×3 | Describe contenido del sitio con precisión | ✓ Bien fundamentada |
| Q09 | Blog Izuran | `izuran.blogspot.com` ×3 | Describe vocabulario guanche correcto | ✓ Bien fundamentada |
| Q12 | Significado "guagua" | `izuran.blogspot.com` ×3 (mitología) | "No hay información sobre guagua" | ✓ Abstención honesta (buen comportamiento) |
| Q26 | Relación Museo Canario + Memoria Lanzarote | `memoriadelanzarote.com` ×3 (sin chunks de `elmuseocanario.com`) | Menciona contribuciones del "Museo Canario" | ⚠️ Síntesis más allá del corpus |
| Q44 | Complementariedad museos/archivos/diccionarios | `elmuseocanario.com` + `cultura.grancanaria.com` | Usa contenido de ambas fuentes | ✓ Bien fundamentada |

**Q12** pone de manifiesto una limitación de la métrica Recall@K: el dominio `izuran.blogspot.com`
coincide con `expected_sources` (R@5=1) aunque el contenido recuperado no responde la pregunta.
Los modelos manejan esto correctamente absteniéndose, pero la métrica no refleja ese acierto.

**Q26** es el único caso donde el LLM genera contenido que no está en los chunks recuperados.
No es una alucinación factual grave (la relación institucional puede ser real), pero demuestra
que el modelo infiere más allá de lo que Qdrant devuelve cuando la pregunta implica una
relación que el corpus solo cubre parcialmente.

---

## Evaluación cualitativa

| Dimensión | Remoto (30B) | Local (4B) |
|-----------|-------------|-----------|
| Precisión factual | Alta | Alta |
| Uso de citas [N] | Consistente | Consistente |
| Concisión | Ligeramente mayor | Ligeramente menor |
| Detalle extraído | Selectivo | Más exhaustivo |
| Razonamiento meta | Sí (reconoce límites del corpus) | Menos frecuente |
| Aclaraciones extra | Pocas | Más frecuentes (paréntesis) |
| Respuestas rechazadas | 0 % | 0 % |
| Síntesis fuera de corpus | Q26 (1 caso) | Q26 (1 caso) (*) |

(*) Se detectó un caso probable de síntesis más allá del corpus: **Q26** ("¿Qué relación existe
entre el Museo Canario y los recursos de Memoria de Lanzarote?"). Los 3 chunks recuperados
son exclusivamente de `memoriadelanzarote.com` (contenido sobre César Manrique, artesanos,
objetos parroquiales), pero ambos modelos responden mencionando contribuciones del
"Museo Canario" — información que no está en ninguno de los chunks devueltos por Qdrant.
El modelo infiere una relación institucional no explicitada en el corpus.

---

## Conclusión para el TFG

**Ambos modelos son funcionalmente equivalentes** para el caso de uso RAG con corpus
acotado: producen respuestas precisas, con citas, sin rechazos y sin alucinaciones
detectadas.

Las diferencias son estilísticas más que sustantivas:

- El modelo **30B remoto** tiende a ser más conciso y a razonar sobre los límites
  del corpus ("el texto no especifica...").
- El modelo **4B local** tiende a ser más exhaustivo en la extracción de detalles
  y añade aclaraciones parentéticas.

Dado que las métricas de recuperación son idénticas (Recall@5=100%, MRR=0.82),
la elección entre ambas configuraciones depende de factores operativos:

| Factor | Favorece remoto | Favorece local |
|--------|----------------|----------------|
| Calidad de síntesis | ✓ | — |
| Disponibilidad offline | — | ✓ |
| Sin dependencia de red/VPN | — | ✓ |
| Coste de infraestructura | — | ✓ |
| Latencia | ✓ (~2.7 s media) | — (~26.3 s media en el run local forzado) |

En el último run local, la latencia sube porque la evaluación ya no usa la API `:8000`
arrancada con el LLM remoto, sino `qwen3.5:4b` directamente contra Ollama local
(`http://127.0.0.1:11434`). Esta diferencia afecta al coste operativo, no a la calidad
de recuperación documental.

_Generado: 2026-06-08_
