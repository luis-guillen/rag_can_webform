# RAGCAN

> **Plataforma ASP.NET Web Forms para crawling, indexación y chat RAG sobre patrimonio de Canarias**  
> Aplicación web con landing, historial de conversaciones y crawler orientado a generar corpus reutilizable.

[![.NET Framework](https://img.shields.io/badge/.NET-Framework%204.8.1-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-7.3-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![ASP.NET Web Forms](https://img.shields.io/badge/ASP.NET-Web%20Forms-0078D4?logo=microsoft)](https://dotnet.microsoft.com/apps/aspnet)
[![Python](https://img.shields.io/badge/Python-3.10+-3776AB?logo=python)](https://python.org/)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.2.3-7952B3?logo=bootstrap)](https://getbootstrap.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## Tabla de Contenidos

- [Descripción](#descripción)
- [Stack Tecnológico](#stack-tecnológico)
- [Arquitectura](#arquitectura)
- [Inicio Rápido](#inicio-rápido)
- [Pipeline completo: crawl → index → chat](#pipeline-completo-crawl--index--chat)
- [Uso y Configuración](#uso-y-configuración)
- [Operación en background (NIVEL 1)](#operación-en-background-nivel-1)
- [Servicio Python RAG (`python/`)](#servicio-python-rag-python)
- [Escalar de 5 a 500 URLs](#escalar-de-5-a-500-urls)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Dependencias NuGet](#dependencias-nuget)

---

## Descripción

**RAGCAN** es una aplicación web ASP.NET Web Forms (.NET Framework 4.8.1) para explorar y consultar un corpus de conocimiento sobre Canarias mediante Retrieval-Augmented Generation (RAG).

El repositorio contiene **dos componentes en un solo repo**:

| Componente | Carpeta | Stack | Función |
|---|---|---|---|
| App web | `/` (raíz) | .NET 4.8.1 / C# | Crawler, indexer, chat UI |
| Servicio RAG | `python/` | Python 3.10+ / FastAPI | Embeddings, Qdrant, API de consulta |

**Flujo de datos:**

```
seeds.txt → [Crawler .NET] → App_Data/crawlings/*.txt + *.metadata.json
                                         ↓
                          [python/run_all.ps1] → Qdrant (vectores)
                                         ↓
                          [Chat.aspx] → python/app/api.py → respuesta RAG
```

**Características clave:**
- Crawler BFS incremental (SHA-256 por página: solo re-crawlea lo que cambia)
- Jobs en **segundo plano** controlables desde la UI: iniciar / parar / progreso / logs en vivo
- Indexación incremental: solo procesa páginas con `needs_index=true`
- Scheduler interno configurable (manual / intervalo / diario)
- Chat RAG con historial persistente y fuentes citadas
- Tema oscuro persistente, landing moderna, Bootstrap 5

---

## Stack Tecnológico

### .NET (app web)

| Categoría | Tecnología | Versión |
|---|---|---|
| Lenguaje | C# | 7.3 |
| Runtime | .NET Framework | 4.8.1 |
| Framework web | ASP.NET Web Forms | — |
| HTML Parsing | HtmlAgilityPack | 1.11.61 |
| Serialización | Newtonsoft.Json | 13.0.3 |
| CSS | Bootstrap | 5.2.3 |
| Iconos | Font Awesome | 6.4.0 (CDN) |
| Servidor dev | IIS Express | — |

### Python (servicio RAG, `python/`)

| Categoría | Tecnología |
|---|---|
| API | FastAPI + uvicorn |
| Embeddings | sentence-transformers (`intfloat/multilingual-e5-small`) |
| Vector DB | Qdrant |
| Chunking | langchain-text-splitters (fallback propio) |
| LLM opcional | OpenAI-compatible (Ollama, OpenRouter, Azure…) |

---

## Arquitectura

### Flujo de crawling (background)

```
[Crawler.aspx] → CrawlerIndexerFacade.StartCrawl()
                        ↓  QueueBackgroundWorkItem
                   CrawlJob.RunAsync()
                        ↓  SemaphoreSlim (MaxConcurrentDomains=10)
              CrawlerService.CrawlDominioAsync()  ×N dominios
                        ↓
              App_Data/crawlings/<dominio>/
                  ├── 01_home.txt                ← texto limpio
                  └── 01_home.metadata.json      ← sidecar con SHA-256, needs_index, etc.
                        ↓
              JobStatusManager → App_Data/status/crawl_status.json
                              → App_Data/status/sources_status.json
                              → App_Data/logs/crawler.log
```

### Flujo de indexación Python (después del crawl)

```
python/run_all.ps1
    ├── app.validate_corpus  → valida pares .txt / .metadata.json
    ├── app.chunk --incremental
    │       Lee needs_index=true en sidecars
    │       Fusiona chunks existentes + chunks nuevos → data/chunks.jsonl
    ├── app.embed_index (upsert, sin recrear colección)
    │       Genera embeddings (multilingual-e5-small)
    │       Upsert en Qdrant (IDs estables por hash)
    ├── scripts/smoke_test_retrieval.py
    └── scripts/acceptance_questions.py
```

### Flujo de consulta RAG

```
[Chat.aspx] → RagQueryService.Ask(pregunta)
                    ↓  POST http://127.0.0.1:8000/query
              python/app/api.py
                    ↓
              Qdrant → top-K chunks → generación (LLM o extractiva)
                    ↓
              { answer, sources, answer_mode }
```

### Capa de servicios .NET (`Services/`)

| Clase | Responsabilidad |
|---|---|
| `CrawlerIndexerFacade` | Punto único de control: Start/Stop/Get para crawl e index |
| `CrawlerService` | Motor BFS: descarga, extrae texto, sigue enlaces |
| `MetadataService` | Genera sidecars `*.metadata.json` con SHA-256, calidad, etc. |
| `Jobs/CrawlJob` | Orquesta crawl incremental por dominio con semáforo |
| `Jobs/IndexJob` | Procesa `needs_index=true`: calcula chunks, registra `last_indexed_at` |
| `Jobs/JobStatusManager` | Estado central thread-safe, single-flight CTS, escritura atómica |
| `Jobs/Scheduler` | Timer in-process (crawl + index programados) |
| `Jobs/JobLogger` | Logs con timestamp UTC y rotación a `.1` al superar 5 MB |
| `RagQueryService` | Llama al API Python y expone health check |
| `ChatHistoryService` | Historial persistente de conversaciones |
| `DuplicateDetector` | SHA-256 por página para detección de contenido duplicado |
| `PathHelper` | Valida y construye rutas ancladas a `App_Data/` |
| `SeedUrlProvider` | Lee URLs semilla desde `seeds.txt` |

---

## Inicio Rápido

### Requisitos

- **Windows** (el proyecto .NET solo compila/ejecuta en Windows)
- Visual Studio 2022 (Community es suficiente) con .NET Framework 4.8.1
- Python 3.10+ (para el servicio RAG)
- Qdrant corriendo en `http://localhost:6333` ([Docker](https://qdrant.tech/documentation/quick-start/): `docker run -p 6333:6333 qdrant/qdrant`)

### 1. Clonar y abrir

```powershell
git clone https://github.com/luis-guillen/rag_can_webform.git
cd rag_can_webform
explorer rag_can_aspx.slnx   # abre Visual Studio
```

### 2. Restaurar NuGet y ejecutar la app .NET

```
Clic derecho en Solución → Restore NuGet Packages
F5 (Debug) o Ctrl+F5 (sin debugger)
```

La app abre en `https://localhost:<puerto>/`. Puertos por defecto: HTTP 5000, HTTPS 44345.

### 3. Preparar el entorno Python

```powershell
cd python
python -m venv .venv
.\.venv\Scripts\activate
pip install -r requirements.txt
```

### 4. Crawlear desde la app web

1. Ir a **Crawler.aspx** (menú → *Crawler*).
2. Pulsar **Iniciar Crawling**. El job corre en background; la UI se actualiza cada 3 s.
3. Esperar a que termine (estado *Completado*).

Los ficheros aparecen en `App_Data/crawlings/<dominio>/`.

### 5. Indexar en Qdrant

```powershell
# Desde rag_can_webform/python/  (con .venv activo)
.\run_all.ps1
```

En el primer arranque hace chunking y embedding completo. En ejecuciones sucesivas solo procesa las páginas nuevas o cambiadas.

### 6. Iniciar la API Python y chatear

```powershell
.\start_api.ps1   # levanta FastAPI en http://127.0.0.1:8000
```

Abrir **Chat.aspx** en la app .NET y hacer preguntas.

---

## Pipeline completo: crawl → index → chat

```
┌──────────────────────────────────────────────────────────────┐
│  1. Crawler.aspx → [Iniciar Crawling]                        │
│     Genera: App_Data/crawlings/**/*.txt + *.metadata.json    │
│     needs_index=true en páginas nuevas o con hash cambiado   │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│  2. python/run_all.ps1  (o run_all.sh en WSL/Linux)          │
│     chunk --incremental  → data/chunks.jsonl (solo lo nuevo) │
│     embed_index (upsert) → Qdrant                            │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│  3. python/start_api.ps1  → http://127.0.0.1:8000            │
│  4. Chat.aspx → preguntas RAG con fuentes                    │
└──────────────────────────────────────────────────────────────┘
```

> **Rebuild completo de Qdrant** (p. ej. tras cambios grandes o mensualmente):
> ```powershell
> python -m app.chunk --full
> python -m app.embed_index --recreate
> ```

---

## Uso y Configuración

### `Web.config` — parámetros del crawler

| Clave | Valor actual | Descripción |
|---|---|---|
| `Crawler:MaxConcurrentDomains` | **10** | Dominios en paralelo (semáforo) |
| `Crawler:RequestDelayMs` | **500** | Pausa entre peticiones HTTP por dominio |
| `Crawler:HttpTimeoutSeconds` | 15 | Timeout por petición |
| `Crawler:MaxPages` | 50 | Límite de páginas por dominio |
| `Crawler:MaxDepth` | 2 | Profundidad máxima de enlaces |
| `Index:ChunkSize` | 1000 | Tamaño de chunk para conteo en el .NET indexer |
| `Rag:QueryEndpoint` | `http://127.0.0.1:8000/query` | URL del API Python |
| `Rag:TopK` | 5 | Número de fragmentos a recuperar |

### `python/.env` — parámetros del servicio Python

Crear `python/.env` para sobreescribir valores (el fichero está en `.gitignore`):

```env
# Corpus (por defecto apunta a App_Data/crawlings/ automáticamente)
RAG_CORPUS_DIR=C:\ruta\alternativa\si\necesaria

# Qdrant
QDRANT_URL=http://localhost:6333
RAG_COLLECTION=rag_canarias

# Embeddings
RAG_EMBED_MODEL=intfloat/multilingual-e5-small
RAG_CHUNK_SIZE=2200
RAG_CHUNK_OVERLAP=250

# LLM opcional (Ollama, OpenAI, etc.)
RAG_LLM_ENABLED=true
RAG_LLM_BASE_URL=http://127.0.0.1:11434
RAG_LLM_API_KEY=ollama
RAG_LLM_MODEL=qwen3.5:4b
```

### Añadir URLs semilla

Editar `App_Data/seeds.txt` (o `Config/seeds.txt`), una URL por línea:

```
https://ejemplo.com
https://otro-dominio.org
```

---

## Operación en background (NIVEL 1)

El crawler y el indexer funcionan como **procesos en segundo plano controlables** desde Web Forms, con estado **persistido en disco**, **crawling e indexado incrementales por hash**, un **scheduler interno** y exposición opcional como **servicio WCF**.

### Métodos públicos (fachada y WCF)

Definidos en `Services/CrawlerIndexerFacade.cs` y expuestos en `Services/Wcf/ICrawlerIndexerService.cs`:

| Método | Descripción |
|---|---|
| `StartCrawl()` | Lanza el crawl de todas las semillas en segundo plano |
| `StartCrawlSource(string url)` | Crawl de una sola URL |
| `StopCrawl()` | Solicita parar el crawl en curso |
| `GetCrawlStatus()` | Estado actual: progreso, URL actual, contadores |
| `StartIndexing()` | Indexa en background solo lo que tiene `needs_index=true` |
| `StopIndexing()` | Solicita parar la indexación |
| `GetSources()` | Lista de fuentes con su estado |
| `GetLogs(int lines)` | Últimas N líneas de `crawler.log` e `indexer.log` |

### Ficheros de estado (`App_Data/`)

| Fichero | Contenido |
|---|---|
| `status/crawl_status.json` | `state`, `progress_percent`, `current_url`, `total/processed/failed/skipped_sources` |
| `status/index_status.json` | Igual para la indexación |
| `status/sources_status.json` | Por URL: `last_crawled_at`, `http_status`, `needs_index`, `pages_total/changed/skipped`, `state` |
| `status/scheduler_config.json` | Modo, intervalo, última/próxima ejecución |
| `logs/crawler.log` | Log del crawler con timestamp UTC |
| `logs/indexer.log` | Log del indexer con timestamp UTC |

### Scheduler

En **Crawler.aspx**, tarjeta *Programación*: modo `manual` / `interval` / `daily`. Un `Timer` in-process revisa la configuración cada minuto y ejecuta el ciclo **crawl → index** si toca y no hay jobs en curso.

> **Nota IIS:** habilitar *Application Initialization* / `AlwaysRunning` en el app pool para que el scheduler no se duerma. Para producción ver [Tarea de Windows](#escalar-de-5-a-500-urls).

### Servicio WCF (opcional)

- Endpoint: `/Services/Wcf/CrawlerIndexerService.svc` (SOAP / `basicHttpBinding`)
- Requiere **WCF HTTP Activation** en *Características de Windows → .NET Framework 4.8 Advanced Services*
- La web y la UI funcionan igualmente sin él

### Robustez

- **Single-flight**: no se permiten dos crawls ni dos indexados simultáneos
- **Try/catch por URL**: una URL fallida no detiene las demás
- **Escritura atómica**: todos los JSON se escriben vía fichero temporal + `File.Replace`
- **Reconciliación en arranque**: `ReconcileOnStartup()` repara estados `running` huérfanos tras un reciclaje del app pool

---

## Servicio Python RAG (`python/`)

El directorio `python/` contiene el servicio FastAPI que genera los embeddings y responde las consultas del chat.

### Estructura

```
python/
├── app/
│   ├── api.py            # FastAPI: /query, /health
│   ├── config.py         # Config central (vars entorno / .env)
│   ├── corpus_utils.py   # Iterador de pares .txt + .metadata.json
│   ├── chunk.py          # Chunking (--incremental / --full)
│   ├── embed_index.py    # Embeddings + upsert Qdrant
│   ├── retrieval.py      # Búsqueda vectorial + alias de fuentes
│   ├── generation.py     # Generación LLM opcional (extractiva si no hay LLM)
│   ├── models.py         # Dataclasses Document, Chunk, DomainMetadata, etc.
│   └── validate_corpus.py # Validación del corpus antes de indexar
├── scripts/
│   ├── smoke_test_retrieval.py
│   └── acceptance_questions.py
├── run_all.ps1 / run_all.sh   # Pipeline completo (valida → chunkea → indexa → tests)
├── start_api.ps1 / start_api.sh
└── requirements.txt
```

### Rutas automáticas

`python/app/config.py` calcula las rutas relativas al repo:

```python
PROJECT_ROOT = Path(__file__).resolve().parent.parent   # → python/
_WEBFORM_ROOT = PROJECT_ROOT.parent                     # → rag_can_webform/
DEFAULT_CORPUS = str(_WEBFORM_ROOT / "App_Data" / "crawlings")
```

No hace falta configurar `RAG_CORPUS_DIR` si la estructura del repo no cambia.

### Modo incremental vs. completo

```powershell
# Incremental (por defecto): solo re-chunkea páginas con needs_index=true
python -m app.chunk --incremental   # o simplemente: python -m app.chunk
python -m app.embed_index           # upsert en Qdrant (sin recrear colección)

# Rebuild completo (primer arranque o limpieza mensual)
python -m app.chunk --full
python -m app.embed_index --recreate
```

**Cómo funciona `--incremental`:**
1. Lee los sidecars `*.metadata.json` y detecta los que tienen `needs_index=true`
2. Carga el `chunks.jsonl` existente y descarta los chunks de esas URLs
3. Re-chunkea solo las páginas cambiadas y escribe el resultado fusionado
4. `embed_index` hace upsert con IDs estables (uuid5): solo los chunks nuevos se embeden realmente

> Los chunks huérfanos de páginas cuyo contenido cambió permanecen en Qdrant hasta el próximo `--recreate`, lo cual es aceptable para ciclos normales de actualización.

---

## Escalar de 5 a 500 URLs

### Qué escala sin cambios

| Componente | Por qué |
|---|---|
| `seeds.txt` | Añadir líneas |
| Hashing incremental | 490 sitios sin cambios → saltados en segundos |
| Qdrant | Diseñado para millones de vectores |
| `sources_status.json` | 500 entradas es insignificante |

### Pasos para 500 URLs

1. **Añadir semillas** a `App_Data/seeds.txt` (o `Config/seeds.txt`).

2. **Concurrencia ya ajustada** en `Web.config`:
   - `MaxConcurrentDomains = 10` → 500 sitios ÷ 10 × ~1.5 min ≈ **~75 min** por ciclo
   - `RequestDelayMs = 500` → más cortés con mayor concurrencia

3. **Primer ciclo Python** (`--full` + `--recreate`). Ciclos sucesivos usan `--incremental`.

4. **Programación robusta para producción:** en lugar del scheduler in-process, crear una **Tarea de Windows** que, en el horario deseado, llame a `StartCrawl` y `StartIndexing` del endpoint WCF (o a una URL de disparo HTTP). Así el ciclo sobrevive a reciclajes del app pool.

5. **GPU para embeddings:** si se dispone de GPU (la RTX 3050 del proyecto la detecta automáticamente), `embed_index` la usa. En CPU, 75.000 chunks tarda ~20-40 min; en GPU, ~2-5 min.

6. **Vectorización real activada:** el hook `IVectorIndexSink` (actualmente `NullVectorIndexSink`) está preparado para conectar el indexer .NET directamente con el API Python sin pasos manuales.

### Ejecutar en el servidor del profesor (IIS)

1. **Publicar** (VS → Build → Publish) a una carpeta del servidor.
2. Crear sitio en IIS: **.NET Framework v4.0**, modo integrado.
3. Dar permisos de **escritura** al app pool sobre `App_Data/`.
4. (Recomendado) Habilitar **Application Initialization** + `AlwaysRunning`.
5. (Opcional) Instalar **WCF HTTP Activation**.
6. Instalar Python + Qdrant en el servidor y ejecutar `run_all.ps1` + `start_api.ps1`.

---

## Estructura del Proyecto

```
rag_can_webform/
├── Landing.aspx / .cs              # Portada principal
├── Chat.aspx / .cs                 # Chat RAG con historial
├── Crawler.aspx / .cs              # Control de crawling (iniciar/parar/estado/logs/scheduler)
├── Indexar.aspx / .cs              # Control de indexado incremental + herramienta de reparación de sidecars
├── Default.aspx / .cs              # Redirige a Crawler.aspx
├── Resultados.aspx / .cs           # Redirige a Crawler.aspx
├── About.aspx / Contact.aspx
├── Site.Master / Site.Mobile.Master
│
├── Services/
│   ├── CrawlerIndexerFacade.cs     # Capa de control (métodos públicos)
│   ├── CrawlerService.cs           # Motor BFS: descarga y extracción de texto
│   ├── CrawlerSettings.cs          # Parámetros de crawling (desde Web.config)
│   ├── MetadataService.cs          # Sidecars *.metadata.json (SHA-256, needs_index…)
│   ├── ChatHistoryService.cs       # Historial de conversaciones
│   ├── DuplicateDetector.cs        # Detección de contenido duplicado
│   ├── PathHelper.cs               # Rutas seguras ancladas a App_Data
│   ├── QualityScorer.cs            # Puntuación de calidad de documentos
│   ├── RagQueryService.cs          # Consulta al API Python + health check
│   ├── SeedUrlProvider.cs          # Proveedor de URLs semilla
│   │
│   ├── Jobs/
│   │   ├── JobStatusModels.cs      # Modelos JSON: JobRunStatus, SourceStatus, SchedulerConfig
│   │   ├── JobStatusManager.cs     # Estado central thread-safe + single-flight + CTS
│   │   ├── JsonFile.cs             # Escritura atómica de JSON
│   │   ├── JobLogger.cs            # Logs con rotación (~5 MB)
│   │   ├── CrawlJob.cs             # Crawl incremental por dominio
│   │   ├── IndexJob.cs             # Indexado incremental (needs_index=true)
│   │   ├── Chunker.cs              # Conteo de chunks (base para Qdrant)
│   │   ├── IVectorIndexSink.cs     # Hook Qdrant (NullVectorIndexSink por defecto)
│   │   └── Scheduler.cs            # Timer in-process
│   │
│   └── Wcf/
│       ├── ICrawlerIndexerService.cs  # Contrato WCF + DTOs
│       └── CrawlerIndexerService.svc(.cs)  # Wrapper WCF de la fachada
│
├── App_Data/
│   ├── seeds.txt                   # URLs semilla (una por línea)
│   ├── crawlings/                  # Salida del crawler (generada en runtime)
│   │   └── <dominio>/
│   │       ├── 01_home.txt         # Texto limpio por página
│   │       └── 01_home.metadata.json  # Sidecar: sha256, needs_index, chunks…
│   ├── status/                     # Estado persistido de jobs y scheduler
│   └── logs/                       # crawler.log, indexer.log
│
├── python/                         # Servicio RAG (FastAPI + Qdrant)
│   ├── app/
│   │   ├── config.py               # Config central (rutas relativas al repo)
│   │   ├── api.py                  # /query, /health
│   │   ├── chunk.py                # --incremental (defecto) / --full
│   │   ├── embed_index.py          # Embeddings + upsert Qdrant
│   │   ├── corpus_utils.py         # Iterador de pares .txt + .metadata.json
│   │   ├── retrieval.py            # Búsqueda vectorial
│   │   ├── generation.py           # LLM opcional / fallback extractivo
│   │   ├── models.py               # Dataclasses
│   │   └── validate_corpus.py
│   ├── scripts/
│   ├── run_all.ps1 / run_all.sh    # Pipeline: valida→chunkea→indexa→tests
│   ├── start_api.ps1 / start_api.sh
│   └── requirements.txt
│
├── Content/
│   ├── bootstrap.css               # Bootstrap 5.2.3
│   └── Site.css                    # Estilos + dark mode tokens
├── Scripts/
│   ├── bootstrap.bundle.js
│   ├── jquery-3.7.0.min.js
│   └── WebForms/ (MSAjax)
├── App_Start/
│   ├── BundleConfig.cs
│   └── RouteConfig.cs
├── Config/
│   └── seeds.txt                   # Alternativa a App_Data/seeds.txt
├── Global.asax / .cs               # EnsureFolders, ReconcileOnStartup, Scheduler.Start
├── Web.config                      # Config .NET + parámetros del crawler
├── packages.config
└── rag_can_aspx.csproj
```

---

## Dependencias NuGet

| Paquete | Versión | Uso |
|---|---|---|
| Bootstrap | 5.2.3 | UI framework |
| jQuery | 3.7.0 | DOM (requerido por WebForms) |
| HtmlAgilityPack | 1.11.61 | DOM parsing y extracción de texto |
| Newtonsoft.Json | 13.0.3 | Serialización JSON |
| Microsoft.AspNet.FriendlyUrls | 1.0.2 | URLs amigables |
| Microsoft.AspNet.Web.Optimization | 1.1.3 | Bundling CSS/JS |
| Microsoft.AspNet.ScriptManager.WebForms | 5.0.0 | Script Manager |
| Microsoft.CodeDom.Providers.DotNetCompilerPlatform | 2.0.1 | Compilador Roslyn |
| Modernizr | 2.8.3 | Detección de características del navegador |

---

## Licencia

Este proyecto está bajo licencia **MIT**. Consulta `LICENSE` para más detalles.

---

**Última actualización:** 2026-06-08 | **Versión:** 1.2 | **Estado:** En desarrollo
