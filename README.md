# RAGCAN

> **Plataforma ASP.NET Web Forms para crawling, indexación, vectorización y chat RAG sobre patrimonio de Canarias**  
> Aplicación web con landing, historial de conversaciones, crawler orientado a corpus, pipeline RAG Python y framework de evaluación automática.

[![.NET Framework](https://img.shields.io/badge/.NET-Framework%204.8.1-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-7.3-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![ASP.NET Web Forms](https://img.shields.io/badge/ASP.NET-Web%20Forms-0078D4?logo=microsoft)](https://dotnet.microsoft.com/apps/aspnet)
[![Python](https://img.shields.io/badge/Python-3.10+-3776AB?logo=python)](https://python.org/)
[![FastAPI](https://img.shields.io/badge/FastAPI-0.136-009688?logo=fastapi)](https://fastapi.tiangolo.com/)
[![Qdrant](https://img.shields.io/badge/Qdrant-vectorDB-DC244C)](https://qdrant.tech/)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.2.3-7952B3?logo=bootstrap)](https://getbootstrap.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## Tabla de Contenidos

- [Descripción](#descripción)
- [Stack Tecnológico](#stack-tecnológico)
- [Arquitectura](#arquitectura)
- [Demo API — arranque con un clic](#demo-api--arranque-con-un-clic)
- [Inicio Rápido](#inicio-rápido)
- [Uso y Configuración](#uso-y-configuración)
- [Evaluación del Sistema](#evaluación-del-sistema)
- [Características Principales](#características-principales)
- [Estructura del Proyecto](#estructura-del-proyecto)

---

## Descripción

**RAGCAN** es una aplicación web ASP.NET Web Forms (.NET Framework 4.8.1) pensada para explorar y consultar un corpus de conocimiento sobre Canarias mediante Retrieval-Augmented Generation (RAG).

La app integra:

- **Landing page** descriptiva como entrada principal
- **Chat RAG** con historial persistente, consulta en tiempo real y visualización de fuentes
- **Crawler BFS** incremental con hash, scheduler y control de jobs en background
- **Indexación** de metadatos y vectorización del corpus en Qdrant con un clic
- **Pipeline RAG Python** (FastAPI + Qdrant + embeddings E5 + LLM Ollama/OpenAI-compatible)
- **Demo API** — botón en navbar que arranca Qdrant Docker + FastAPI con detección automática de LLM remoto o local
- **Framework de evaluación** automática con 50 preguntas, métricas Recall@K / MRR y comparativa local/remoto

**Resultado de evaluación sobre 50 preguntas de patrimonio canario:**

| Métrica | LLM remoto (qwen3:30b) | LLM local (qwen3.5:4b) |
|---------|------------------------|------------------------|
| **Recall@5** | **100.0 %** | **100.0 %** |
| **MRR** | **0.8190** | **0.8190** |
| Latencia media | 2 715 ms | 26 283 ms |
| Rechazadas | 0 % | 0 % |

> Las métricas de recuperación son idénticas en ambas configuraciones: Recall@K y MRR dependen
> del índice Qdrant y los embeddings, no del LLM empleado para la generación.

---

## Stack Tecnológico

### Backend web (C#)

| Categoría | Tecnología | Versión |
|-----------|-----------|---------|
| Lenguaje | C# | 7.3 |
| Runtime | .NET Framework | 4.8.1 |
| Web Framework | ASP.NET Web Forms | — |
| HTML Parsing | HtmlAgilityPack | 1.11.61 |
| Serialización | Newtonsoft.Json | 13.0.3 |
| Servidor | IIS Express | — |
| Compilador | Roslyn (csc.exe) | 2.0.1 |

### Pipeline RAG (Python)

| Categoría | Tecnología | Versión |
|-----------|-----------|---------|
| API | FastAPI + uvicorn | 0.136+ |
| Vector DB | Qdrant | Docker `qdrant/qdrant` |
| Embeddings | intfloat/multilingual-e5-small | 384 dims |
| Aceleración | PyTorch CUDA 12.8 | 2.7+ |
| LLM (remoto) | qwen3:30b-a3b (Ollama, Dell Pro Max) | — |
| LLM (local) | qwen3.5:4b (Ollama local) | — |
| Evaluación | scripts/run_evaluation.py | — |

### Frontend

| Tecnología | Versión |
|-----------|---------|
| Bootstrap | 5.2.3 |
| Font Awesome | 6.4.0 (CDN) |
| jQuery | 3.7.0 |

---

## Arquitectura

### Flujo completo del sistema

```
[Usuario] ──► Landing.aspx ──► Chat.aspx
                                   │ POST /query
                                   ▼
                          FastAPI :8000 (python/.venv)
                                   │
                    ┌──────────────┴──────────────┐
                    ▼                             ▼
             Qdrant :6333                  LLM (Ollama)
          rag_canarias (501 pts)      remoto :11434 Dell Pro Max
          multilingual-e5-small        o local :11434
```

### Flujo de indexación y vectorización

```
[Crawler.aspx] ──► App_Data/crawlings/<dominio>/*.txt + *.metadata.json
                                   │
                         [Indexar.aspx]
                         "Chunk + Vectorizar en Qdrant"
                                   │
                    python scripts/run_evaluation.py
                    python -m app.chunk --full
                    python -m app.embed_index
                                   │
                             Qdrant collection
                             rag_canarias (501 puntos)
```

### Flujo de evaluación

```
[Evaluacion.aspx]
  Botón "Evaluar — LLM remoto"  ──► run_evaluation.py --label remote --llm-base-url Llm:RemoteUrl --llm-model Llm:RemoteModel
  Botón "Evaluar — LLM local"   ──► run_evaluation.py --label local  --llm-base-url Llm:LocalUrl  --llm-model Llm:LocalModel
                                          │
                               POST /query × 50 preguntas
                                          │
                          evaluation/results_{label}.json
                          evaluation/report_{label}.md
                          evaluation/tfg_tables_{label}.md
                          evaluation/comparison.md
```

### Capa de Servicios (`Services/`)

| Clase | Responsabilidad |
|-------|----------------|
| `ChatHistoryService` | Persistencia, carga y listado del historial de conversaciones |
| `CrawlerService` | Motor BFS: descarga, extrae texto, sigue enlaces |
| `CrawlerSettings` | Validación y encapsulación de parámetros del formulario |
| `CrawlJobManager` | Gestión del estado del trabajo de crawling |
| `DuplicateDetector` | Evita procesar URLs duplicadas o ya visitadas |
| `MetadataService` | Genera y actualiza `metadata.json` desde el corpus |
| `QualityScorer` | Puntúa documentos por calidad de texto |
| `RagQueryService` | Consulta al servicio RAG y exposición del estado de salud |
| `SeedUrlProvider` | Lee y provee las URLs semilla desde `App_Data/seeds.txt` |
| `PathHelper` | Centraliza la construcción de rutas dentro de `App_Data/` |

---

## Demo API — arranque con un clic

El botón **Demo API** en la navbar arranca todo el stack Python con un clic:

1. Detecta si el **LLM remoto** (Dell Pro Max `10.17.159.197:11434`) es alcanzable; si no, usa Ollama local.
2. Arranca **Qdrant** en Docker (`qdrant/qdrant`, puertos 6333/6334, volumen `python/qdrant_storage/`).
3. Arranca **FastAPI** con el `.venv` de `python/` (o repo hermano `rag_can_python` como fallback).
4. El indicador del botón cambia:
   - **Gris** — API apagada
   - **Amarillo** — iniciando...
   - **Azul** ⚡ remoto — API activa con LLM Dell Pro Max
   - **Verde** 💻 local — API activa con LLM Ollama local

La configuración de LLM vive en `Web.config`:

```xml
<add key="Llm:RemoteUrl"   value="http://10.17.159.197:11434" />
<add key="Llm:RemoteModel" value="qwen3:30b-a3b-instruct-2507-q4_K_M" />
<add key="Llm:LocalUrl"    value="http://127.0.0.1:11434" />
<add key="Llm:LocalModel"  value="qwen3.5:4b" />
```

**Implementación:** `DemoApi.ashx` — handler `IHttpHandler` con acciones `start`, `stop`, `status`. El proceso FastAPI se mantiene referenciado en `Application["DemoApi:Process"]`.

---

## Inicio Rápido

### Requisitos Previos

- **Visual Studio 2019+** (Community es suficiente)
- **.NET Framework 4.8.1** SDK
- **Docker Desktop** (para Qdrant)
- **Python 3.10+** con venv en `python/` (o `rag_can_python/`)
- **Ollama** para el LLM local (opcional; se auto-detecta el remoto si está disponible)

### Instalación

1. **Clonar el repositorio**
   ```powershell
   git clone https://github.com/luis-guillen/rag_can_webform.git
   cd rag_can_webform
   ```

2. **Restaurar dependencias NuGet y abrir en Visual Studio**
   ```powershell
   explorer rag_can_aspx.slnx
   # Clic derecho en Solución → Restore NuGet Packages
   ```

3. **Preparar el entorno Python**
   ```powershell
   cd python
   python -m venv .venv
   .\.venv\Scripts\Activate.ps1
   pip install -r requirements.txt
   ```

4. **Ejecutar la webapp**
   - Presionar **F5** en Visual Studio
   - HTTP: puerto variable (ver `.vs/config/applicationhost.config`)

5. **Arrancar la Demo API**
   - Pulsar el botón **Demo API** en la navbar
   - Esperar a que el indicador se ponga azul (remoto) o verde (local)

6. **Vectorizar el corpus** (primera vez)
   - Ir a **Indexar** → botón **"Chunk + Vectorizar en Qdrant"**
   - Espera ~2-3 min; genera 532 chunks → 501 puntos en Qdrant

---

## Uso y Configuración

### Parámetros del Crawler

| Parámetro | Tipo | Rango | Defecto | Descripción |
|-----------|------|-------|---------|------------|
| `url` | text | N/A | vacío | URL a rastrear. Si vacía, se usan seeds de `App_Data/seeds.txt`. |
| `carpeta` | text | N/A | `crawlings/` | Subcarpeta de salida dentro de `App_Data/`. |
| `maxPages` | int | 1–10000 | 50 | Máximo número de páginas a descargar. |
| `maxDepth` | int | 0–10 | 2 | Profundidad máxima de enlaces a seguir. |
| `fullCrawl` | bool | — | false | Permite hasta 1000 páginas. |

### Variables de entorno del pipeline Python

| Variable | Valor por defecto | Descripción |
|----------|------------------|-------------|
| `RAG_LLM_ENABLED` | `true` | Activa síntesis generativa |
| `RAG_LLM_BASE_URL` | auto-detectado | Endpoint Ollama/OpenAI-compatible |
| `RAG_LLM_MODEL` | auto-detectado | Nombre del modelo |
| `RAG_LLM_TIMEOUT_SECONDS` | `90` | Timeout de generación |
| `RAG_LLM_MAX_TOKENS` | `300` | Tokens máximos de respuesta |
| `QDRANT_URL` | `http://localhost:6333` | Endpoint Qdrant |
| `RAG_COLLECTION` | `rag_canarias` | Colección Qdrant |
| `RAG_EMBED_MODEL` | `intfloat/multilingual-e5-small` | Modelo de embeddings |

---

## Evaluación del Sistema

La página **Evaluación** ejecuta 50 preguntas de patrimonio cultural canario y calcula métricas estándar de recuperación.
Cada botón fuerza su endpoint/modelo de LLM desde `Web.config`, así que la evaluación local usa Ollama local aunque la Demo API esté activa contra el LLM remoto.

### Métricas globales (corpus: 315 páginas, 501 puntos Qdrant)

| Métrica | LLM remoto (qwen3:30b) | LLM local (qwen3.5:4b) |
|---------|------------------------|------------------------|
| **Recall@1** | **72.0 %** | **72.0 %** |
| **Recall@3** | **92.0 %** | **92.0 %** |
| **Recall@5** | **100.0 %** | **100.0 %** |
| **MRR** | **0.8190** | **0.8190** |
| Latencia media | 2 715 ms | 26 283 ms |
| Latencia p90 | 3 780 ms | 36 179 ms |
| Con fuentes | 100.0 % | 100.0 % |
| Rechazadas | 0.0 % | 0.0 % |

### Por tipo de pregunta (LLM remoto)

| Tipo | N | Recall@5 | MRR | Lat. media |
|------|---|---------|-----|-----------|
| Recuperación directa | 15 | 100.0 % | 0.8167 | 2 546 ms |
| Síntesis | 10 | 100.0 % | 0.8250 | 2 723 ms |
| Multifuente | 10 | 100.0 % | 0.9500 | 2 707 ms |
| Semántica | 8 | 100.0 % | 0.7396 | 3 111 ms |
| Razonamiento | 7 | 100.0 % | 0.7190 | 2 627 ms |

### Archivos generados

| Archivo | Contenido |
|---------|-----------|
| `python/evaluation/results_remote.json` | Resultados brutos + métricas (LLM remoto) |
| `python/evaluation/results_local.json` | Resultados brutos + métricas (LLM local) |
| `python/evaluation/report_remote.md` | Informe Markdown (LLM remoto) |
| `python/evaluation/report_local.md` | Informe Markdown (LLM local) |
| `python/evaluation/tfg_tables_remote.md` | Tablas LaTeX para TFG (LLM remoto) |
| `python/evaluation/tfg_tables_local.md` | Tablas LaTeX para TFG (LLM local) |
| `python/evaluation/comparison.md` | Comparativa completa ambas configuraciones |

### Ejecutar evaluación

Desde la webapp: **Evaluación** → botón **"Evaluar — LLM remoto"** o **"Evaluar — LLM local"**.

Desde consola:
```powershell
cd python
.\.venv\Scripts\Activate.ps1
python scripts/run_evaluation.py --label remote   # o --label local
```

---

## Características Principales

### Crawling Inteligente
- Algoritmo BFS con control de profundidad e incremental por hash SHA-256
- Restricción automática a dominio único; filtro de URLs binarias
- Delay configurable entre peticiones (politeness)
- Jobs controlables (iniciar / parar) desde la UI, con estado persistido en disco

### Pipeline RAG Python
- Chunking configurable (~2200 chars, overlap 250) con filtro de páginas ruidosas
- Embeddings `intfloat/multilingual-e5-small` con GPU CUDA si disponible
- Qdrant como vector store local en Docker
- Detección automática de fuente por alias en la pregunta (filtro suave + fallback)
- LLM generativo opcional (Ollama, OpenAI-compatible); fallback extractivo si falla

### Demo API (un clic)
- Detecta LLM remoto (Dell Pro Max) o cae a local automáticamente
- Arranca Qdrant Docker si no está corriendo
- Indicador visual en navbar con estado en tiempo real (polling cada 6 s)

### Vectorización desde la UI
- Botón en Indexar.aspx lanza `app.chunk --full` + `app.embed_index` en background
- Progreso en tiempo real via timer + FileShare.ReadWrite
- Log disponible en `python/vectorizar.log`

### Evaluación Automática
- 50 preguntas clasificadas por dificultad (easy/medium/hard/expert) y tipo
- Métricas: Recall@1/3/5, MRR, latencia avg/p90, % fuentes, % rechazadas
- Dos botones para comparar LLM remoto vs local
- Resultados en tabs Bootstrap, tablas LaTeX para TFG
- Comparativa completa en `python/evaluation/comparison.md`

### Interfaz Web
- Tema oscuro persistente (localStorage + cookie fallback), sin parpadeo al cargar
- Navbar con botón Demo API y indicador de estado con color
- Responsive Bootstrap 5

---

## Estructura del Proyecto

```
rag_can_webform/
├── Landing.aspx                   # Portada principal
├── Chat.aspx / .cs                # Chat RAG
├── Crawler.aspx / .cs             # UI de crawling: control, estado, logs, scheduler
├── Indexar.aspx / .cs             # Indexación + vectorización Qdrant
├── Evaluacion.aspx / .cs          # Framework de evaluación (50 preguntas, Recall@K, MRR)
├── DemoApi.ashx                   # Handler IHttpHandler: arranca/para Qdrant+FastAPI
├── Resultados.aspx / .cs          # Resumen de crawling
├── Default.aspx / .cs             # Compatibilidad; redirige a Crawler.aspx
├── About.aspx                     # Página informativa
├── Contact.aspx                   # Redirige a Landing.aspx
├── Site.Master                    # Layout + navbar + Demo API button + dark mode
├── Site.Mobile.Master             # Master page móvil
├── Services/
│   ├── ChatHistoryService.cs
│   ├── CrawlerService.cs
│   ├── CrawlerSettings.cs
│   ├── CrawlJobManager.cs
│   ├── DuplicateDetector.cs
│   ├── MetadataService.cs
│   ├── PathHelper.cs
│   ├── QualityScorer.cs
│   ├── RagQueryService.cs
│   ├── SeedUrlProvider.cs
│   └── Jobs/
│       ├── CrawlJob.cs
│       ├── IndexJob.cs
│       ├── JobStatusManager.cs
│       ├── JobStatusModels.cs
│       ├── JobLogger.cs
│       ├── JsonFile.cs
│       ├── Chunker.cs
│       ├── IVectorIndexSink.cs
│       └── Scheduler.cs
├── python/                        # Pipeline RAG Python (self-contained)
│   ├── .venv/                     # Entorno virtual Python (local al repo)
│   ├── app/
│   │   ├── api.py                 # FastAPI /query + /health
│   │   ├── chunk.py               # Chunking + filtro de ruido
│   │   ├── config.py              # Configuración (env / .env)
│   │   ├── corpus_utils.py        # Lectura del corpus ASP.NET
│   │   ├── embed_index.py         # Embeddings → Qdrant
│   │   ├── generation.py          # Síntesis LLM + fallback extractivo
│   │   ├── models.py              # Pydantic models
│   │   ├── query.py               # CLI de consulta
│   │   ├── retrieval.py           # Lógica de búsqueda (CLI + API)
│   │   └── validate_corpus.py
│   ├── data/
│   │   └── evaluation/
│   │       └── questions.json     # 50 preguntas con expected_sources
│   ├── evaluation/                # Generado por run_evaluation.py
│   │   ├── results_remote.json
│   │   ├── results_local.json
│   │   ├── report_remote.md
│   │   ├── report_local.md
│   │   ├── tfg_tables_remote.md
│   │   ├── tfg_tables_local.md
│   │   └── comparison.md          # Comparativa completa para TFG
│   ├── scripts/
│   │   └── run_evaluation.py      # Runner de evaluación (--label local|remote)
│   ├── qdrant_storage/            # Datos persistentes de Qdrant (Docker volume)
│   ├── requirements.txt
│   ├── start_api.ps1              # Arranca Qdrant + FastAPI (Windows)
│   ├── start_api.sh               # Arranca Qdrant + FastAPI (Linux/WSL)
│   ├── run_all.ps1                # Valida + chunks + embed + smoke tests (Windows)
│   └── run_all.sh                 # Ídem (Linux/WSL)
├── App_Data/
│   ├── seeds.txt                  # URLs semilla
│   ├── status/                    # Estado de jobs (JSON)
│   ├── logs/                      # Logs de crawler e indexer
│   └── crawlings/                 # Corpus crawleado
│       └── <dominio>/
│           ├── NNN_*.txt
│           ├── NNN_*.metadata.json
│           └── metadata.json
├── Content/
│   ├── bootstrap.css
│   └── Site.css                   # Dark mode tokens + responsive fixes
├── Scripts/
│   ├── bootstrap.bundle.js
│   ├── jquery-3.7.0.min.js
│   └── ...
├── Web.config                     # Config ASP.NET + LLM endpoints
├── packages.config
├── rag_can_aspx.csproj
└── README.md
```

---

## Operación en background: crawler/indexer controlables

El crawler y el indexer funcionan como **procesos en segundo plano controlables** con estado persistido en disco, crawling e indexado incrementales por hash, scheduler interno y servicio WCF opcional.

### Métodos públicos (fachada)

| Método | Descripción |
|--------|-------------|
| `StartCrawl()` | Lanza el crawl de todas las semillas |
| `StopCrawl()` | Para el crawl en curso |
| `GetCrawlStatus()` | Estado actual (progreso, URL actual, contadores) |
| `StartIndexing()` | Indexa solo los documentos con `needs_index=true` |
| `StopIndexing()` | Para la indexación |
| `GetLogs(int lines)` | Últimas N líneas de `crawler.log` / `indexer.log` |

### Ficheros de estado (`App_Data/`)

| Fichero | Contenido |
|---------|-----------|
| `status/crawl_status.json` | Estado del crawl: state, progreso, URL actual, contadores |
| `status/index_status.json` | Estado de la indexación |
| `status/sources_status.json` | Una entrada por URL con hash, needs_index, chunk_count |
| `status/scheduler_config.json` | Configuración del scheduler |
| `logs/crawler.log`, `logs/indexer.log` | Logs con rotación (~5 MB) |

---

## Dependencias NuGet

| Paquete | Versión |
|---------|---------|
| Bootstrap | 5.2.3 |
| jQuery | 3.7.0 |
| HtmlAgilityPack | 1.11.61 |
| Newtonsoft.Json | 13.0.3 |
| Microsoft.AspNet.FriendlyUrls | 1.0.2 |
| Microsoft.AspNet.Web.Optimization | 1.1.3 |
| Microsoft.CodeDom.Providers.DotNetCompilerPlatform | 2.0.1 |

---

## Licencia

Este proyecto está bajo licencia **MIT**. Consulta `LICENSE` para más detalles.

---

**Última actualización:** 2026-06-08 | **Estado:** En desarrollo (TFG)
