# RAG Canarias — módulo Python

Pipeline RAG local que **consume** el corpus generado por la app ASP.NET Web Forms
(`rag_can_webform`) y expone un endpoint FastAPI listo para que `Chat.aspx` lo consulte.

```
ASP.NET Web Forms (crawler/indexer/chat)         Python (este módulo)
─────────────────────────────────────         ──────────────────────────
 App_Data/fix_museo_canario_sin_dupes/<dominio>/NNN_*.txt    ───┐
 App_Data/fix_museo_canario_sin_dupes/<dominio>/NNN_*.metadata.json ──►  validate_corpus
                                       │       chunk → data/chunks.jsonl
                                       │       embed_index → Qdrant
                                       │       FastAPI :8000/query
 Chat.aspx ─────────── HTTP POST ──────────────► /query → {answer, sources, answer_mode}
```

## Arquitectura en una página

1. **Validación** (`app/validate_corpus.py`) — recorre el corpus en disco, comprueba que
   cada `.txt` tiene su `.metadata.json` con `domain_metadata` y `page_metadata`, y reporta
   válidos / inválidos / vacíos / huérfanos / duplicados. No modifica archivos. Los `.txt`
   sin metadata se reportan como advertencia por defecto porque el chunking solo consume pares
   válidos; usa `--strict-orphans` si quieres tratarlos como error.
2. **Chunking** (`app/chunk.py`) — divide cada documento en trozos de ~2200 caracteres con
   solapamiento de 250, respetando párrafos cuando es posible
   (`langchain_text_splitters.RecursiveCharacterTextSplitter`).
   Filtra automáticamente páginas ruidosas (legales, feeds, búsquedas) y documentos
   excesivamente grandes (>500 K chars, típicamente binarios mal interpretados).
   Persiste `data/chunks.jsonl` con `chunk_id`, `source_id`, `text` y `metadata`.
3. **Indexación** (`app/embed_index.py`) — carga `intfloat/multilingual-e5-small` (384 dims),
   usa GPU si `torch.cuda.is_available()`, antepone el prefijo `passage:` requerido por E5
   y sube los puntos a la colección Qdrant `rag_canarias`.
4. **Consulta CLI** (`app/query.py`) — embedding de la pregunta (con prefijo `query:`),
   detección automática de fuentes conocidas, búsqueda top-K con filtro duro o suave y
   pretty-print por terminal.
5. **API** (`app/api.py`) — FastAPI con `GET /health` y `POST /query`. Si `RAG_LLM_ENABLED`
   está activo y la configuración OpenAI-compatible es válida, sintetiza una respuesta en
   español usando solo los chunks recuperados. Si el LLM está desactivado o falla, la respuesta
   es **extractiva**: concatena los pasajes más relevantes con su fuente. `sources` siempre se
   devuelve estructurado (Chat.aspx puede renderizarlo aparte).

## Estructura

```
rag_can_python/
├── app/
│   ├── __init__.py
│   ├── api.py            # FastAPI /query + /health
│   ├── chunk.py          # chunking + filtro de ruido
│   ├── config.py         # toda la configuración (env / .env)
│   ├── corpus_utils.py   # lectura del corpus ASP.NET
│   ├── embed_index.py    # embeddings → Qdrant
│   ├── generation.py     # síntesis LLM opcional + fallback extractivo
│   ├── models.py         # Pydantic: Document, Chunk, Source, QueryRequest…
│   ├── query.py          # CLI de consulta
│   ├── retrieval.py      # lógica de búsqueda compartida (CLI + API)
│   └── validate_corpus.py
├── scripts/
│   └── smoke_test_retrieval.py   # tests T1–T4 contra Qdrant en vivo
├── data/                 # generado: chunks.jsonl, etc.
├── requirements.txt
├── README_RAG.md
└── .env                  # opcional, ver «Configuración»
```

## Configuración

Toda la configuración vive en `app/config.py` y puede sobrescribirse vía variables de
entorno o un fichero `.env` en la raíz del repo.

| Variable                | Default                                                      | Descripción                                       |
| ----------------------- | ------------------------------------------------------------ | ------------------------------------------------- |
| `RAG_CORPUS_DIR`        | `C:\Users\jaime\...\rag_can_webform\App_Data\fix_museo_canario_sin_dupes` | Raíz del corpus ASP.NET             |
| `RAG_DATA_DIR`          | `./data`                                                     | Salida de chunks                                  |
| `QDRANT_URL`            | `http://localhost:6333`                                      | Endpoint Qdrant                                   |
| `QDRANT_API_KEY`        | *(vacío)*                                                    | Solo en cloud / con auth                          |
| `RAG_COLLECTION`        | `rag_canarias`                                               | Colección Qdrant                                  |
| `RAG_EMBED_MODEL`       | `intfloat/multilingual-e5-small`                             | Modelo de embeddings (384 dims)                   |
| `RAG_CHUNK_SIZE`        | `2200`                                                       | Tamaño máximo de chunk en caracteres              |
| `RAG_CHUNK_OVERLAP`     | `250`                                                        | Overlap entre chunks                              |
| `RAG_MIN_CHUNK_CHARS`   | `150`                                                        | Descarta chunks con menos caracteres no-espacio   |
| `RAG_MAX_DOC_CHARS`     | `500000`                                                     | Descarta documentos sospechosamente grandes       |
| `RAG_NOISE_PATTERNS`    | *(ver abajo)*                                                | Patrones de ruido, separados por coma             |
| `RAG_SOURCE_ALIASES`    | *(ver abajo)*                                                | Alias `nombre=dominio`, separados por `;`         |
| `RAG_TOP_K`             | `5`                                                          | Top-K por defecto                                 |
| `RAG_ALLOWED_ORIGINS`   | `http://localhost,http://127.0.0.1`                          | Orígenes CORS                                     |
| `RAG_LLM_ENABLED`       | `false`                                                      | Activa síntesis generativa opcional               |
| `RAG_LLM_BASE_URL`      | *(vacío)*                                                    | Endpoint OpenAI-compatible                        |
| `RAG_LLM_API_KEY`       | *(vacío)*                                                    | API key del proveedor                             |
| `RAG_LLM_MODEL`         | *(vacío)*                                                    | Modelo de chat                                    |
| `RAG_LLM_TIMEOUT_SECONDS` | `60`                                                       | Timeout de generación                             |
| `RAG_LLM_MAX_CONTEXT_CHARS` | `12000`                                                  | Límite de contexto enviado al LLM                 |

### Filtro de ruido (`RAG_NOISE_PATTERNS`)

Páginas cuya URL o título contengan alguno de estos patrones se excluyen de la indexación:

```
aviso-legal, aviso legal, politica, privacidad, cookies,
login, buscar, search, tags, feed, rss
```

Para incluirlas explícitamente al generar chunks:

```powershell
python -m app.chunk --include-noisy
```

Para personalizar la lista (reemplaza los defaults):

```
RAG_NOISE_PATTERNS=aviso-legal,privacidad,cookies,login,rss
```

### Alias de fuentes (`RAG_SOURCE_ALIASES`)

Cuando la pregunta menciona explícitamente una fuente conocida, el retrieval prioriza
resultados de ese dominio. Aliases por defecto:

| Mención en la pregunta            | Dominio priorizado                    |
| --------------------------------- | ------------------------------------- |
| `canarias azul` / `iatext`        | `canarias-azul.iatext.ulpgc.es`       |
| `memoria de lanzarote`            | `memoriadelanzarote.com`              |
| `museo canario` / `el museo canario` | `elmuseocanario.com`               |
| `academia canaria de la lengua` / `academia canaria` | `www.academiacanarialengua.org` |
| `cultura gran canaria` / `cultura de gran canaria` | `cultura.grancanaria.com`   |
| `izuran`                          | `izuran.blogspot.com`                 |

Para añadir aliases sin reemplazar los defaults, define `RAG_SOURCE_ALIASES` con el
formato `alias1=dominio1;alias2=dominio2;...`.

## Instalación (Windows + PowerShell)

```powershell
cd C:\Users\jaime\source\repos\luis-guillen\rag_can_python

# 1) venv
python -m venv .venv
.\.venv\Scripts\Activate.ps1

# 2) Dependencias (incluye PyTorch con CUDA 12.8 si tienes RTX 3050)
python -m pip install --upgrade pip
pip install -r requirements.txt
```

> Si la instalación de torch/cu128 falla porque tu driver no soporta CUDA 12.8, edita
> `requirements.txt` y comenta el bloque `--index-url ... torch ... cu128`; pip instalará la
> versión CPU. El módulo detecta GPU/CPU automáticamente.

## Levantar Qdrant local en Docker

```powershell
docker run -d --name qdrant `
  -p 6333:6333 -p 6334:6334 `
  -v qdrant_storage:/qdrant/storage `
  qdrant/qdrant
```

UI web: <http://localhost:6333/dashboard>

## Flujo completo (paso a paso)

```powershell
# 1) Validar el corpus generado por ASP.NET
python -m app.validate_corpus

# 2) Generar chunks → data/chunks.jsonl
python -m app.chunk

# 3) Embeddings + indexación en Qdrant (la primera vez, --recreate)
python -m app.embed_index --recreate

# 4) Probar por terminal
python -m app.query "¿Qué sabes de Memoria de Lanzarote?"

# 5) Arrancar la API
.\start_api.ps1
```

> Los scripts también funcionan como ficheros directos (`python app\query.py "..."`),
> pero recomendamos `python -m app.<script>` para que los imports relativos funcionen
> sin sorpresas.

## CLI de consulta (`app/query.py`)

```
python -m app.query <pregunta> [opciones]

Opciones:
  --top-k N          Número de resultados (default: 5)
  --domain DOMINIO   FILTRO DURO: solo resultados de ese dominio, sin fallback
  --no-detect        Desactiva la detección automática de fuentes
  --inspect          Muestra info del payload Qdrant y sale (debug)
  --json             Salida en JSON estructurado
```

Ejemplos:

```powershell
# Búsqueda global
python -m app.query "yacimientos arqueológicos en Gran Canaria"

# Auto-detección de fuente ("Museo Canario" → elmuseocanario.com, filtro suave + fallback)
python -m app.query "¿Qué hace el Museo Canario?"

# Filtro duro: solo resultados de un dominio concreto
python -m app.query "arqueología" --domain elmuseocanario.com

# Inspeccionar estado del payload en Qdrant
python -m app.query --inspect
```

### Modos de filtrado

| Modo | Cómo se activa | Comportamiento |
|---|---|---|
| Global | Sin `--domain` ni fuente detectada | Búsqueda sin filtro en toda la colección |
| Filtro suave | Fuente detectada automáticamente | Resultados del dominio primero; si no llena top-K, rellena con fallback global (`is_fallback=true`) |
| Filtro duro | `--domain X` explícito | Solo resultados de ese dominio, nunca fallback |

## Probar la API

- Healthcheck: <http://127.0.0.1:8000/health>
- Docs interactivas (Swagger): <http://127.0.0.1:8000/docs>

```powershell
curl.exe -X POST http://127.0.0.1:8000/query `
  -H "Content-Type: application/json" `
  -d '{ "question": "¿Qué sabes de Memoria de Lanzarote?", "top_k": 5 }'
```

## Arranque diario

1. Comprueba que Qdrant está levantado en `http://localhost:6333`.
2. Comprueba que Ollama está levantado en `http://127.0.0.1:11434` con el modelo `qwen3.5:4b`.
3. En `rag_can_python`, ejecuta `.\start_api.ps1`.
4. Abre WebForms. `Chat.aspx` consume `http://127.0.0.1:8000/query` y muestra el estado de `/health`.

`start_api.ps1` activa por defecto el LLM local:

```powershell
RAG_LLM_ENABLED=true
RAG_LLM_BASE_URL=http://127.0.0.1:11434
RAG_LLM_MODEL=qwen3.5:4b
RAG_LLM_TIMEOUT_SECONDS=90
RAG_LLM_MAX_CONTEXT_CHARS=1800
RAG_LLM_MAX_TOKENS=300
```

Respuesta esperada (resumen):

```json
{
  "answer": "Sobre «...», esto es lo más relevante del corpus indexado: ...",
  "answer_mode": "extractive",
  "diagnostics": {
    "llm_enabled": false,
    "fallback_reason": "RAG_LLM_ENABLED no está activo"
  },
  "sources": [
    {
      "score": 0.81,
      "title": "Memoria de Lanzarote",
      "url": "https://memoriadelanzarote.com/",
      "domain": "memoriadelanzarote.com",
      "source_name": "Memoria de Lanzarote",
      "text_preview": "...",
      "is_fallback": false
    }
  ]
}
```

### Síntesis LLM opcional

`/query` funciona sin API key. Para activar síntesis generativa, configura un proveedor
OpenAI-compatible:

```powershell
$env:RAG_LLM_ENABLED="true"
$env:RAG_LLM_BASE_URL="https://api.openai.com/v1"
$env:RAG_LLM_API_KEY="..."
$env:RAG_LLM_MODEL="gpt-4o-mini"
uvicorn app.api:app --host 127.0.0.1 --port 8000 --reload
```

La respuesta generativa se construye solo con los chunks recuperados y pide citas por número.
Si el proveedor falla, `/query` no cae: devuelve `answer_mode="extractive"` y registra el
motivo en `diagnostics`.

## Smoke tests

Ejecuta los tests de regresión contra el Qdrant local (requiere colección ya indexada):

```powershell
python scripts/smoke_test_retrieval.py
```

Tests incluidos:

| Test | Qué verifica |
|---|---|
| T1 | `--domain elmuseocanario.com` nunca devuelve resultados de otro dominio |
| T2 | `--domain canarias-azul.iatext.ulpgc.es` nunca devuelve resultados de otro dominio |
| T3 | Auto-detección «Museo Canario»: resultados no-fallback antes que fallbacks, todos del dominio correcto |
| T4 | `inspect_collection()` confirma que el payload contiene la clave `domain` con valor no vacío |

> T1 y T3 se saltan automáticamente si `elmuseocanario.com` no tiene chunks en la colección.

## Aceptación operativa

Ejecuta las seis preguntas canónicas y valida que recuperen el dominio esperado:

```powershell
python scripts/acceptance_questions.py
```

Los scripts `run_all.ps1` y `run_all.sh` ejecutan validación de corpus, chunking, recreación
del índice, smoke tests y aceptación. Al terminar muestran el comando exacto para levantar
FastAPI.

## Conectar `Chat.aspx`

Desde el code-behind C# de `Chat.aspx.cs`, llama al endpoint con `HttpClient`:

```csharp
using System.Net.Http;
using Newtonsoft.Json;

private static readonly HttpClient _http = new HttpClient {
    BaseAddress = new Uri("http://localhost:8000/")
};

protected async void BtnPreguntar_Click(object sender, EventArgs e)
{
    var payload = new {
        question = TxtPregunta.Text,
        top_k = 5
    };

    var json = JsonConvert.SerializeObject(payload);
    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

    var resp = await _http.PostAsync("query", content);
    resp.EnsureSuccessStatusCode();

    var body = await resp.Content.ReadAsStringAsync();
    dynamic data = JsonConvert.DeserializeObject(body);

    LblRespuesta.Text = data.answer;
    // data.sources → lista con score, title, url, domain, text_preview, is_fallback
}
```

Notas:

- El servidor permite CORS desde `localhost`/`127.0.0.1` por regex en `config.ALLOWED_ORIGINS`.
- En Web Forms, normalmente *no* necesitas CORS porque la llamada se hace desde el servidor
  ASP.NET, no desde el navegador. Si en algún momento llamas vía AJAX del cliente, ajusta
  `RAG_ALLOWED_ORIGINS` con el origen real (p.ej. `http://localhost:44380`).

## Modelo y dimensiones

- Default: `intfloat/multilingual-e5-small` → 384 dims, multilingüe (incluye español).
- Si cambias a otro modelo (p.ej. `e5-base`, `bge-m3`, ...), recuerda:
  1. ajustar `RAG_EMBED_MODEL`,
  2. **recrear** la colección con `--recreate`,
  3. re-indexar todo el corpus.

## Limitaciones conocidas

- **elmuseocanario.com**: el crawler ASP.NET solo extrae el `<title>` de estas páginas
  (el cuerpo parece renderizarse vía JavaScript). Los 44 documentos del dominio tienen
  media de 55 caracteres, todos por debajo de `RAG_MIN_CHUNK_CHARS=150`, por lo que
  no generan ningún chunk. La corrección requiere mejorar el crawler (headless browser
  o mejor extractor HTML).

## Despliegue en Dell Pro Max (NVIDIA GB10 / DGX Spark) con Docker

Para servir la API a un frontend WebForms en otra máquina (p. ej. por VPN) desde
un Dell Pro Max con GB10 (ARM64 + Blackwell, memoria unificada), se conteneriza el
stack completo (API + Qdrant + Ollama).

Ficheros (en `python/`): `Dockerfile`, `docker-compose.yml`, `requirements-app.txt`, `.env.example`.
Ejecuta los comandos `docker compose` desde `python/` (es el contexto de build).
Requiere `nvidia-container-toolkit` en el host (DGX OS suele traerlo).

```bash
cd python

# 1. Configuración
cp .env.example .env          # ajusta RAG_ALLOWED_ORIGINS (origen del WebForms) y RAG_CORPUS_HOST_DIR

# 2. Build de la API (imagen base NGC PyTorch ARM64; primera vez ~20+ GB)
docker compose build api

# 3. Gate CUDA: debe imprimir True y NVIDIA GB10
docker compose run --rm api python -c "import torch; print(torch.__version__, torch.version.cuda, torch.cuda.is_available(), torch.cuda.get_device_name(0))"

# 4. Infra + modelo LLM (persisten en volúmenes)
docker compose up -d qdrant ollama
docker compose exec ollama ollama pull qwen2.5:14b-instruct

# 5a. Indexar en el contenedor (corpus montado en /data/corpus)
docker compose run --rm api bash -lc "python -m app.validate_corpus && python -m app.chunk && python -m app.embed_index --recreate"
# 5b. (alternativa) restaurar un volumen qdrant_storage ya generado en otra máquina.

# 6. Arrancar la API (bind 0.0.0.0:8000)
docker compose up -d api

# 7. Verificar
curl -s http://localhost:8000/health        # status ok, qdrant_points > 0, answer_mode llm
```

Notas:
- `QDRANT_URL` y `RAG_LLM_BASE_URL` se fijan a DNS de servicio (`qdrant`, `ollama`)
  en `docker-compose.yml`; no toques esos en `.env`.
- **CORS por VPN:** con `allow_credentials=True` no vale `*`. Pon el origen exacto
  del WebForms en `RAG_ALLOWED_ORIGINS` y/o ajusta `RAG_ALLOWED_ORIGIN_REGEX`.
- Abre el puerto TCP 8000 en el firewall y rutas de la VPN.
- En `Chat.aspx` apunta `Rag:QueryEndpoint` / `Rag:HealthEndpoint` (en `Web.config`)
  a `http://<IP-VPN-del-Dell>:8000/query` y `/health`.
- `qwen2.5:14b-instruct` es el arranque recomendado para 128 GB; sube a 32B si quieres.

## Lo que NO incluye esta versión (a propósito)

- Reranking (cross-encoder).
- Crawl4AI / RAGAS / evaluación automática.
- Auth en la API (se asume localhost-only).

## Roadmap inmediato

1. Añadir endpoint `POST /reindex` para que la pestaña «Indexación» de Web Forms dispare
   chunking + embedding sin tocar consola.
2. Añadir un reranker ligero (`BAAI/bge-reranker-v2-m3`) sobre los top-50.
3. Investigar extracción de contenido de `elmuseocanario.com` (JS-rendering).
