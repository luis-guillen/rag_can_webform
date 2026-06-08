# RAG Canarias — módulo Python

Pipeline RAG local que **consume** el corpus generado por la app ASP.NET Web Forms
(`rag_can_webform`) y expone un endpoint FastAPI listo para que `Chat.aspx` lo consulte.

```
ASP.NET Web Forms (crawler/indexer/chat)         Python (este módulo)
─────────────────────────────────────         ──────────────────────────
 App_Data/crawlings/<dominio>/NNN_*.txt ──────► app.chunk --full
 App_Data/crawlings/<dominio>/NNN_*.metadata.json    │
                                                app.embed_index
                                                     │ 501 puntos
                                               Qdrant :6333
                                                     │
 Chat.aspx ──── HTTP POST ───────────────► FastAPI :8000/query
                                           → {answer, sources, answer_mode}
```

## Resultados de evaluación

**50 preguntas · corpus 315 páginas · 501 puntos Qdrant**

| Métrica | LLM remoto (qwen3:30b) | LLM local (qwen3.5:4b) |
|---------|------------------------|------------------------|
| **Recall@5** | **100.0 %** | **100.0 %** |
| **MRR** | **0.8190** | **0.8190** |
| Recall@1 | 72.0 % | 72.0 % |
| Recall@3 | 92.0 % | 92.0 % |
| Latencia media | 2 715 ms | 26 283 ms |
| Rechazadas | 0 % | 0 % |

> Recall@K y MRR son independientes del LLM: dependen del índice Qdrant y los embeddings.
> Ver `evaluation/comparison.md` para la comparativa completa con tablas LaTeX.

---

## Arquitectura

1. **Validación** (`app/validate_corpus.py`) — comprueba que cada `.txt` tiene su `.metadata.json`, reporta huérfanos y duplicados sin modificar archivos.
2. **Chunking** (`app/chunk.py`) — divide cada documento en trozos de ~2200 chars con overlap 250, filtra páginas ruidosas (legales, feeds, búsquedas), persiste `data/chunks.jsonl`.
3. **Indexación** (`app/embed_index.py`) — carga `intfloat/multilingual-e5-small` (384 dims), usa GPU CUDA si disponible, sube puntos a la colección Qdrant `rag_canarias`.
4. **Consulta CLI** (`app/query.py`) — embedding de pregunta, detección automática de fuentes conocidas, búsqueda top-K con filtro duro o suave.
5. **API** (`app/api.py`) — FastAPI con `GET /health` y `POST /query`. Síntesis generativa con LLM si `RAG_LLM_ENABLED=true`; fallback extractivo si el LLM falla.
6. **Evaluación** (`scripts/run_evaluation.py`) — 50 preguntas con Recall@K, MRR, latencia; guarda resultados etiquetados (`--label remote|local`).

---

## Estructura

```
python/
├── .venv/                          # Entorno virtual (self-contained, ~2.7 GB con PyTorch)
├── app/
│   ├── __init__.py
│   ├── api.py                      # FastAPI /query + /health  ← NO MODIFICAR
│   ├── chunk.py                    # Chunking + filtro de ruido  ← NO MODIFICAR
│   ├── config.py                   # Configuración (env / .env)
│   ├── corpus_utils.py             # Lectura del corpus ASP.NET
│   ├── embed_index.py              # Embeddings → Qdrant  ← NO MODIFICAR
│   ├── generation.py               # Síntesis LLM + fallback extractivo  ← NO MODIFICAR
│   ├── models.py                   # Pydantic models
│   ├── query.py                    # CLI de consulta
│   ├── retrieval.py                # Lógica de búsqueda  ← NO MODIFICAR
│   └── validate_corpus.py
├── data/
│   ├── chunks.jsonl                # Generado por app.chunk (532 chunks)
│   └── evaluation/
│       └── questions.json          # 50 preguntas con expected_sources y metadatos
├── evaluation/                     # Generado por run_evaluation.py
│   ├── results_remote.json         # Métricas + respuestas (qwen3:30b)
│   ├── results_local.json          # Métricas + respuestas (qwen3.5:4b)
│   ├── report_remote.md            # Informe Markdown (LLM remoto)
│   ├── report_local.md             # Informe Markdown (LLM local)
│   ├── tfg_tables_remote.md        # Tablas LaTeX TFG (LLM remoto)
│   ├── tfg_tables_local.md         # Tablas LaTeX TFG (LLM local)
│   ├── comparison.md               # Comparativa completa con LaTeX
│   ├── run_remote.log              # Log del último run remoto
│   └── run_local.log               # Log del último run local
├── scripts/
│   ├── run_evaluation.py           # Runner de evaluación (--label local|remote)
│   ├── smoke_test_retrieval.py     # Tests T1–T4 contra Qdrant en vivo
│   └── acceptance_questions.py     # 6 preguntas canónicas de aceptación
├── qdrant_storage/                 # Datos persistentes de Qdrant (volume Docker)
├── requirements.txt
├── start_api.ps1                   # Arranca Qdrant + FastAPI (Windows)
├── start_api.sh                    # Arranca Qdrant + FastAPI (Linux/WSL)
├── run_all.ps1                     # Valida + chunks + embed + smoke (Windows)
├── run_all.sh                      # Ídem (Linux/WSL)
└── README_RAG.md
```

---

## Configuración

Toda la configuración vive en `app/config.py` y puede sobreescribirse vía variables de entorno o fichero `.env`.

| Variable | Default | Descripción |
|----------|---------|-------------|
| `RAG_CORPUS_DIR` | `App_Data/crawlings` | Raíz del corpus ASP.NET |
| `RAG_DATA_DIR` | `./data` | Salida de chunks |
| `QDRANT_URL` | `http://localhost:6333` | Endpoint Qdrant |
| `RAG_COLLECTION` | `rag_canarias` | Colección Qdrant |
| `RAG_EMBED_MODEL` | `intfloat/multilingual-e5-small` | Modelo embeddings (384 dims) |
| `RAG_CHUNK_SIZE` | `2200` | Tamaño máximo de chunk (chars) |
| `RAG_CHUNK_OVERLAP` | `250` | Overlap entre chunks |
| `RAG_TOP_K` | `5` | Top-K por defecto |
| `RAG_LLM_ENABLED` | `false` | Activa síntesis generativa |
| `RAG_LLM_BASE_URL` | — | Endpoint Ollama/OpenAI-compatible |
| `RAG_LLM_MODEL` | — | Nombre del modelo |
| `RAG_LLM_TIMEOUT_SECONDS` | `90` | Timeout de generación |
| `RAG_LLM_MAX_TOKENS` | `300` | Tokens máximos |
| `RAG_LLM_MAX_CONTEXT_CHARS` | `1800` | Límite de contexto al LLM |

### LLM remoto (Dell Pro Max) vs local (Ollama)

Entorno usado en las evaluaciones:

| Rol | Equipo / servicio | Especificación observada |
|-----|-------------------|--------------------------|
| Local | Acer Nitro AN515-45 | AMD Ryzen 7 5800H, 8C/16T, 16 GB RAM, NVIDIA GeForce RTX 3050 Laptop GPU 4 GB VRAM |
| Remoto | Dell Pro Max vía Ollama | API `http://10.17.159.197:11434`, Ollama `0.24.0`, modelo `qwen3:30b-a3b-instruct-2507-q4_K_M` |

La API remota de Ollama informa que el modelo remoto es GGUF, familia `qwen3moe`,
`30.5B` parámetros, cuantización `Q4_K_M` y tamaño aproximado de 18.6 GB. No expone
CPU, RAM ni GPU de la Dell Pro Max, así que esos datos de hardware no se infieren.

Los scripts `start_api.ps1` / `start_api.sh` **auto-detectan** el LLM disponible:

```
1. Intenta conectar a http://10.17.159.197:11434/api/tags (timeout 3 s)
   → OK:  usa qwen3:30b-a3b-instruct-2507-q4_K_M
   → KO:  usa Ollama local qwen3.5:4b en http://127.0.0.1:11434
```

También se puede forzar manualmente:
```powershell
$env:RAG_LLM_BASE_URL = "http://127.0.0.1:11434"
$env:RAG_LLM_MODEL    = "qwen3.5:4b"
.\start_api.ps1
```

---

## Instalación

### Windows (PowerShell)

```powershell
cd python

# 1) Entorno virtual self-contained
python -m venv .venv
.\.venv\Scripts\Activate.ps1

# 2) Dependencias (PyTorch CUDA 12.8 ~2.7 GB; usa cache si ya instalado en otro venv)
pip install -r requirements.txt --prefer-binary
```

> `requirements.txt` usa `--extra-index-url` para el índice de PyTorch, manteniendo PyPI
> como índice primario (necesario para fastapi, uvicorn y el resto de paquetes).

### Linux/WSL

```bash
cd python
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt --prefer-binary
```

---

## Levantar Qdrant en Docker

```powershell
docker run -d --name qdrant `
  --restart unless-stopped `
  -p 6333:6333 -p 6334:6334 `
  -v "$PWD/qdrant_storage:/qdrant/storage" `
  qdrant/qdrant
```

UI web: <http://localhost:6333/dashboard>

El botón **Demo API** de la webapp hace esto automáticamente.

---

## Flujo completo (primera vez)

```powershell
# Con .venv activado, desde python/

# 1) Validar corpus
python -m app.validate_corpus

# 2) Generar chunks (532 chunks desde 315 páginas)
python -m app.chunk --full

# 3) Embeddings + indexación Qdrant (501 puntos)
python -m app.embed_index --recreate

# 4) Probar por terminal
python -m app.query "¿Qué sabes de Memoria de Lanzarote?"

# 5) Arrancar FastAPI (detecta LLM remoto/local automáticamente)
.\start_api.ps1
```

O desde la webapp: **Indexar** → **"Chunk + Vectorizar en Qdrant"** hace los pasos 2+3.

---

## Evaluación automática

```powershell
# Con FastAPI activa en :8000
python scripts/run_evaluation.py --label remote   # usa LLM remoto
python scripts/run_evaluation.py --label local    # usa LLM local
# Sin --label: auto-detecta del /health
python scripts/run_evaluation.py
```

Genera en `evaluation/`:
- `results_{label}.json` — resultados brutos + métricas
- `report_{label}.md` — informe Markdown
- `tfg_tables_{label}.md` — tablas LaTeX para memoria TFG

También desde la webapp: **Evaluación** → botones **"Evaluar — LLM remoto/local"**.

---

## CLI de consulta

```
python -m app.query <pregunta> [opciones]

Opciones:
  --top-k N          Número de resultados (default: 5)
  --domain DOMINIO   Filtro duro: solo ese dominio
  --no-detect        Desactiva detección automática de fuentes
  --json             Salida JSON estructurada
```

### Modos de filtrado

| Modo | Activación | Comportamiento |
|------|-----------|----------------|
| Global | Sin --domain ni fuente detectada | Búsqueda sin filtro |
| Filtro suave | Fuente detectada en pregunta | Dominio primero; rellena con fallback si no alcanza top-K |
| Filtro duro | `--domain X` explícito | Solo ese dominio, sin fallback |

### Alias de fuentes predefinidos

| Mención | Dominio |
|---------|---------|
| `museo canario` | `elmuseocanario.com` |
| `memoria de lanzarote` | `memoriadelanzarote.com` |
| `canarias azul` / `iatext` | `canarias-azul.iatext.ulpgc.es` |
| `academia canaria` | `www.academiacanarialengua.org` |
| `cultura gran canaria` | `cultura.grancanaria.com` |
| `izuran` | `izuran.blogspot.com` |

---

## Probar la API

```powershell
# Health check
curl.exe http://127.0.0.1:8000/health

# Consulta
curl.exe -X POST http://127.0.0.1:8000/query `
  -H "Content-Type: application/json" `
  -d '{"question": "¿Qué sabes de los guanches?", "top_k": 5}'

# Docs interactivos (Swagger)
# http://127.0.0.1:8000/docs
```

---

## Smoke tests y aceptación

```powershell
# Tests T1-T4 contra Qdrant en vivo
python scripts/smoke_test_retrieval.py

# 6 preguntas canónicas de aceptación operativa
python scripts/acceptance_questions.py
```

| Test | Qué verifica |
|------|-------------|
| T1 | `--domain elmuseocanario.com` nunca devuelve otro dominio |
| T2 | `--domain canarias-azul.iatext.ulpgc.es` ídem |
| T3 | Auto-detección «Museo Canario»: no-fallback antes que fallback |
| T4 | Payload Qdrant contiene clave `domain` no vacía |

---

## Arranque diario

1. Docker Desktop activo (Qdrant se arranca solo vía Demo API o `start_api.ps1`)
2. Ollama activo si se quiere LLM local (`ollama serve` o desde la bandeja de Windows)
3. En la webapp: **Demo API** → encender (detecta remoto/local automáticamente)
4. O desde consola: `cd python && .\start_api.ps1`

---

## Limitaciones conocidas

- **elmuseocanario.com**: el crawler extrae solo el `<title>` (cuerpo vía JavaScript). Los 44 documentos tienen media 55 chars — por debajo de `RAG_MIN_CHUNK_CHARS=150`, no generan chunks. Requiere headless browser para solucionarlo.
- El scheduler interno depende de que el app pool de IIS esté activo. Para producción, usar Tarea programada de Windows.

## Archivos restringidos (no modificar)

`app/api.py`, `app/retrieval.py`, `app/generation.py`, `app/chunk.py`, `app/embed_index.py`
