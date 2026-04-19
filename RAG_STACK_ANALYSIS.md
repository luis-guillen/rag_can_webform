# Python vs C# para RAG: análisis por capa (2026)

> Análisis del stack actual y recomendaciones de migración parcial para construir
> un RAG potente y generalizable a múltiples dominios web.
> Actualizado con benchmarks y herramientas de 2026.

---

## Resumen ejecutivo

No hay que reescribir todo. La recomendación es **mantener C# para la capa de adquisición
de datos y la UI**, y **delegar a Python todo el pipeline de inteligencia** (chunking,
embeddings, reranking, LLM, evaluación). La integración se hace mediante `metadata.json`
como contrato de datos compartido y una API REST mínima en Python (FastAPI).

---

## Análisis capa por capa

### ✅ MANTENER EN C# — Ya implementado y adecuado

| Capa | Estado | Veredicto | Razón |
|---|---|---|---|
| **Crawler BFS** (`CrawlerService.cs`) | ✅ Hecho | Mantener | HtmlAgilityPack es sólido; migrar a Scrapy solo aporta si se escala a miles de dominios con proxies o SPAs |
| **Limpieza HTML** | ✅ Hecho | Mantener | Lógica ajustada a dominios canarios; Python (trafilatura) sería mejor solo si se añaden muchos dominios nuevos |
| **Metadatos** (`MetadataService.cs`) | ✅ Hecho | Mantener | `metadata.json` es el contrato entre ambas capas |
| **Calidad + deduplicación** | ✅ Hecho | Mantener | Lógica trivial, igual en cualquier lenguaje |
| **UI Web** (Default/Indexar/Resultados) | ✅ Hecho | Mantener | Herramienta interna; reescribir en Flask no aporta valor |

**Cuándo reconsiderar el crawler en Python:** si necesitas crawlear webs con JavaScript
(SPAs), manejar CAPTCHAs, rotar proxies o escalar a >100 dominios. En ese caso,
[Scrapy](https://scrapy.org/) + [Playwright](https://playwright.dev/python/) sería la
combinación adecuada.

---

### ⚡ HACER EN PYTHON — Lo que queda por implementar

---

#### 1. Chunking semántico

**Veredicto: Python es claramente superior.**

| Herramienta | Por qué Python gana aquí |
|---|---|
| `tiktoken` (OpenAI) | Cuenta tokens exactos del modelo target; no existe equivalente estable en .NET |
| `langchain.text_splitter` | `RecursiveCharacterTextSplitter` con solapamiento configurable, battle-tested |
| `llama-index` node parsers | `SemanticSplitterNodeParser` con embeddings para cortes semánticos |
| `semantic-text-splitter` | Rust-based, muy rápido, bindings Python; sin equivalente .NET |

**Estrategia recomendada 2026:** recursive chunking con **512 tokens y 100 tokens de
solapamiento**. Según estudios NAACL 2025, el chunking semántico cuesta 3-5× más
computacionalmente sin mejoras consistentes frente al recursivo bien configurado.

```python
from langchain.text_splitter import RecursiveCharacterTextSplitter
import tiktoken

enc = tiktoken.encoding_for_model("text-embedding-3-small")
splitter = RecursiveCharacterTextSplitter.from_tiktoken_encoder(
    chunk_size=512, chunk_overlap=100,
    separators=["\n\n", "\n", ". ", " "]
)
chunks = splitter.split_text(texto)
```

**Late chunking (técnica emergente 2026):** desarrollado por Jina AI, el transformer
procesa el documento completo antes de dividirlo, por lo que cada chunk lleva contexto
global. Mejora la coherencia en documentos largos.

---

#### 2. Embeddings

**Veredicto: Python es claramente superior.**

En .NET solo está disponible la API de OpenAI/Azure. En Python tienes además modelos
locales de última generación:

| Modelo | Params | MTEB Multilingüe | Contexto | Coste | Recomendación |
|---|---|---|---|---|---|
| **Qwen3-Embedding-8B** | 8B | **70.58 (#1 MTEB 2025)** | 32K tokens | Gratis (local) | 🥇 Mejor calidad multilingüe |
| **BGE-M3** | 570M | Alta | 8192 tokens | Gratis (local) | 🥈 Mejor relación calidad/recursos |
| `multilingual-e5-large` | 560M | Muy buena en ES | 512 tokens | Gratis (local) | Alternativa ligera |
| `text-embedding-3-small` | - | Alta | 8192 tokens | ~$0.02/M tokens | Si no quieres gestionar GPU |
| GTE-Qwen2-7B | 7B | 65.4 MTEB | **131K tokens** | Gratis (local) | Para documentos muy largos |

**Recomendación para este proyecto (corpus en español):**
- Con GPU disponible: `Qwen3-Embedding-8B` — #1 en MTEB multilingüe
- Sin GPU / solo CPU: `BGE-M3` — soporta dense + sparse + multi-vector en un solo modelo,
  ideal para hybrid search sin modelos separados

```python
from sentence_transformers import SentenceTransformer

model = SentenceTransformer("BAAI/bge-m3")
embeddings = model.encode(chunks, batch_size=32, show_progress_bar=True)
```

---

#### 3. Vector Store

**Veredicto: ambos tienen clientes, pero Python tiene más opciones maduras para indexación.**

| Store | Latencia p50 | Hybrid search | Self-hosted | .NET client | Recomendación |
|---|---|---|---|---|---|
| **Qdrant** | ~6 ms | ✅ | ✅ Docker | ✅ Oficial | 🥇 Para este proyecto |
| **Weaviate** | ~8 ms | ✅ nativo BM25+vector | ✅ | ✅ | Mejor arquitectura híbrida |
| **Pinecone** | ~4.2 ms | ✅ | ❌ Cloud only | ✅ | Si no quieres infraestructura |
| **pgvector** | ~12 ms | ⚠️ manual | ✅ PostgreSQL | ✅ Npgsql | Si ya tienes PostgreSQL |
| **FAISS** | <1 ms | ❌ | ✅ | ❌ | Solo Python, para prototipos |
| **TiDB Vector** | - | ✅ + SQL | ✅ | ✅ | SQL + vector en uno, multi-tenant |

**Recomendación:** **Qdrant en Docker**. Indexación desde Python, consultas desde C# via
REST. La arquitectura de producción 2026 recomienda hybrid search (BM25 + vector) con
reciprocal rank fusion — Qdrant y Weaviate lo implementan de forma nativa.

```bash
docker run -p 6333:6333 -v $(pwd)/qdrant_storage:/qdrant/storage qdrant/qdrant
```

---

#### 4. Reranking

**Veredicto: Python es claramente superior. No existe alternativa .NET madura.**

El reranking es el paso que más mejora la precisión del RAG (+10-20% relevancia).
Tomas los top-20 del vector store y los reordenas con un modelo cross-encoder más preciso.

| Modelo | Params | Tipo | Multilingüe | Coste | Notas 2026 |
|---|---|---|---|---|---|
| **Chroma Context-1** | 20B | LLM especializado RAG | ✅ | API | 🥇 Mejor precisión, modelo dedicado a RAG |
| **BGE-Reranker-v2-M3** | 568M | Cross-encoder | ✅ 100+ idiomas | Gratis local | 🥈 Mejor relación coste/calidad para español |
| Cohere Rerank v3 | - | API | ✅ | De pago | Excelente, sin infraestructura |
| `ms-marco-MiniLM-L-6` | 22M | Cross-encoder | ❌ Solo inglés | Gratis local | Solo si el corpus es en inglés |

**Chroma Context-1** (2026) es un modelo de 20B parámetros entrenado específicamente
para decidir qué documentos son más relevantes para una query RAG. Supera a los
cross-encoders clásicos pero requiere más recursos o uso via API.

**Para este proyecto:** `BGE-Reranker-v2-M3` como baseline gratis y local, con opción de
migrar a Chroma Context-1 si se necesita mayor precisión.

```python
from sentence_transformers import CrossEncoder

reranker = CrossEncoder("BAAI/bge-reranker-v2-m3")
pares = [(query, chunk.text) for chunk in top_20_chunks]
scores = reranker.predict(pares)
top_5 = [chunk for _, chunk in sorted(zip(scores, top_20_chunks), reverse=True)][:5]
```

---

#### 5. Orquestación RAG

**Veredicto: Python gana. El patrón 2026 es LlamaIndex para indexación + LangGraph para agentes.**

| Framework | Stars GitHub | Latencia RAG | Token usage | Mejor para |
|---|---|---|---|---|
| **LlamaIndex** | 44K | ~6 ms | ~1.60K | RAG puro, indexación, retrieval, 300+ conectores |
| **LangChain + LangGraph** | 119K | ~10 ms | ~2.40K | Agentes complejos, 500+ integraciones, cadenas |
| **Haystack** | - | ~5.9 ms | ~1.57K | Producción, pipelines modulares, búsqueda QA |
| **DSPy** | - | - | - | Optimización automática de prompts |
| Semantic Kernel (C#) | - | - | - | Solo si te quedas 100% en .NET/Azure |

**Patrón recomendado 2026:** LlamaIndex para indexación y retrieval + LangGraph para la
capa de agente/orquestación. Ambos se integran de forma nativa.

```
LlamaIndex (chunking + indexación + query engine)
    ↕
LangGraph (agente, memoria, multi-step reasoning)
    ↕
Qdrant (vector store)
```

---

#### 6. LLM

**Veredicto: equivalente en ambos lenguajes; Python integra mejor con los frameworks.**

| Modelo | Contexto | Razonamiento | Coste | Recomendación |
|---|---|---|---|---|
| **Claude Sonnet 4.6** | 200K | Muy alto | Medio | 🥇 Mejor para RAG con documentos largos |
| GPT-4o | 128K | Alto | Medio | Alternativa sólida |
| Llama 3.3 70B (local) | 128K | Alto | Gratis | Si los datos no pueden salir |
| Gemini 2.0 Flash | 1M | Alto | Bajo | Corpus muy grandes |

Para RAG en español con contexto cultural canario: **Claude Sonnet 4.6** — mejor
comprensión de matices lingüísticos del español y capacidad de citar fuentes con precisión.

---

#### 7. Evaluación (RAGAS)

**Veredicto: solo existe en Python. No hay alternativa en .NET.**

[RAGAS](https://docs.ragas.io/) es el estándar de facto. Procesa +5M evaluaciones/mes
(AWS, Microsoft, Databricks). Imprescindible para saber si añadir un nuevo dominio
mejora o degrada el sistema.

| Métrica | Qué mide |
|---|---|
| **Faithfulness** | ¿La respuesta es fiel al contexto recuperado? |
| **Answer Relevancy** | ¿La respuesta responde la pregunta? |
| **Context Precision** | ¿Se recuperaron los fragmentos correctos? |
| **Context Recall** | ¿Se recuperó toda la información necesaria? |

```python
from ragas import evaluate
from ragas.metrics import faithfulness, answer_relevancy, context_recall

results = evaluate(
    dataset=test_dataset,  # preguntas sobre cultura canaria
    metrics=[faithfulness, answer_relevancy, context_recall]
)
print(results)  # scores 0-1 por métrica
```

---

## Arquitectura recomendada final (2026)

```
┌──────────────────────────────────────────────────────────────┐
│                    CAPA C# (actual)                          │
│  Crawler (HtmlAgilityPack) → Limpieza → Metadatos            │
│  metadata.json  ←→  App_Data/{corpus}/*.txt                  │
│  UI: Default.aspx · Indexar.aspx · Buscar.aspx (nueva)       │
└─────────────────────────┬────────────────────────────────────┘
                          │ metadata.json + .txt files
                          ▼
┌──────────────────────────────────────────────────────────────┐
│                   CAPA PYTHON (nueva)                        │
│                                                              │
│  scripts/                                                    │
│  ├── chunk.py     # RecursiveTextSplitter (512t / 100 overlap)│
│  ├── embed.py     # Qwen3-8B o BGE-M3 → vectors             │
│  ├── index.py     # sube chunks + metadatos a Qdrant         │
│  ├── rerank.py    # BGE-Reranker-v2-M3                       │
│  ├── eval.py      # RAGAS sobre preguntas de prueba          │
│  └── api.py       # FastAPI: POST /query → respuesta + fuentes│
│                                                              │
│  Stack: llama-index · sentence-transformers · qdrant-client  │
│         langchain · ragas · fastapi · anthropic              │
└─────────────────────────┬────────────────────────────────────┘
                          │ Docker
                          ▼
              ┌──────────────────────┐
              │  Qdrant (self-hosted)│
              │  Hybrid: dense+BM25  │
              └──────────────────────┘
```

---

## Plan de implementación incremental

| Paso | Qué hacer | Herramienta | Esfuerzo |
|---|---|---|---|
| 1 | Leer `metadata.json` + `.txt` → `chunks.jsonl` | LlamaIndex / langchain | Bajo |
| 2 | Generar embeddings → subir a Qdrant | BGE-M3 + qdrant-client | Bajo |
| 3 | Query manual desde terminal (sin LLM) | qdrant-client | Bajo |
| 4 | Añadir reranker | BGE-Reranker-v2-M3 | Bajo |
| 5 | FastAPI `POST /query` con Claude | fastapi + anthropic | Medio |
| 6 | Evaluación RAGAS (20-30 preguntas canarias) | ragas | Medio |
| 7 | `Buscar.aspx` en C# llama a FastAPI | ASP.NET | Medio |
| 8 | Generalizar: nuevo dominio → re-crawl → re-eval | Seeds.txt + RAGAS | Bajo |

---

## Tabla resumen de decisiones

| Componente | Lenguaje | Herramienta 2026 | Motivo |
|---|---|---|---|
| Crawler + limpieza HTML | **C#** ✅ | HtmlAgilityPack (actual) | Ya hecho, suficiente para webs estáticas |
| Metadatos, calidad, dedup | **C#** ✅ | MetadataService (actual) | Integrado con la UI |
| UI de administración | **C#** ✅ | ASP.NET Web Forms | Sin cambios necesarios |
| Chunking | **Python** | `RecursiveCharacterTextSplitter` (LangChain) | tiktoken, sin rival en .NET |
| Embeddings | **Python** | `Qwen3-Embedding-8B` / `BGE-M3` | #1 MTEB multilingüe, gratis, local |
| Vector store (indexación) | **Python** | Qdrant | Hybrid search nativo, Docker, REST |
| Hybrid search | **Python** | Qdrant (dense + BM25 nativo) | Mejor precisión que solo vectorial |
| Reranking | **Python** | `BGE-Reranker-v2-M3` → Chroma Context-1 | Cross-encoder multilingual gratuito |
| Orquestación RAG | **Python** | LlamaIndex + LangGraph | LlamaIndex mejor para retrieval puro |
| LLM | **Python** | Claude Sonnet 4.6 | 200K contexto, mejor en español |
| Evaluación | **Python** | RAGAS | No existe en .NET |
| UI de búsqueda/chat | **C#** | `Buscar.aspx` → llama a FastAPI | Sin reescritura del frontend |

---

## Referencias (2026)

- [Best Embedding Models 2026 - PE Collective](https://pecollective.com/blog/best-embedding-models-2026/)
- [Qwen3 Embedding: #1 MTEB Multilingual](https://qwenlm.github.io/blog/qwen3-embedding/)
- [Comparative Analysis Qwen3 vs BGE-M3 - Medium](https://medium.com/@mrAryanKumar/comparative-analysis-of-qwen-3-and-bge-m3-embedding-models-for-multilingual-information-retrieval-72c0e6895413)
- [Best Vector Databases for RAG 2026 - TiDB](https://www.pingcap.com/compare/best-vector-database/)
- [Best Vector Databases 2026 - AlphaCorp](https://alphacorp.ai/blog/best-vector-databases-for-rag-2026-top-7-picks)
- [Building Modern RAG 2026: Qwen3 + Qdrant - Towards AI](https://pub.towardsai.net/building-a-modern-rag-pipeline-in-2026-qwen3-embeddings-and-vector-database-in-qdrant-ebeca2bbe338)
- [Best RAG Frameworks 2026: LangChain vs LlamaIndex vs DSPy](https://iternal.ai/blockify-rag-frameworks)
- [LangChain vs LlamaIndex 2026 - Morph](https://www.morphllm.com/morphllm/comparisons/langchain-vs-llamaindex)
- [LlamaIndex vs LangChain vs Haystack - Kanerika](https://kanerika.com/blogs/llamaindex-vs-langchain-vs-haystack/)
- [Chroma Context-1: RAG-specialized model - MindStudio](https://www.mindstudio.ai/blog/what-is-chroma-context-1-specialized-rag-model)
- [Best Reranker Models 2026 - BSWEN](https://docs.bswen.com/blog/2026-02-25-best-reranker-models/)
- [BGE Reranker Cross-Encoder 2026 - MarkAICode](https://markaicode.com/bge-reranker-cross-encoder-reranking-rag/)
- [RAG Pipelines in Production: benchmarks - DEV Community](https://dev.to/pooyagolchian/rag-pipelines-in-production-vector-database-benchmarks-chunking-strategies-and-hybrid-search-data-gbl)
- [Best Chunking Strategies 2025 - Firecrawl](https://www.firecrawl.dev/blog/best-chunking-strategies-rag)
- [Advanced RAG - Hugging Face Cookbook](https://huggingface.co/learn/cookbook/en/advanced_rag)
- [RAGAS documentation](https://docs.ragas.io/)
- [Hybrid Search RAG - Meilisearch](https://www.meilisearch.com/blog/hybrid-search-rag)
