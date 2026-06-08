# RAGCAN

> **Plataforma ASP.NET Web Forms para crawling, indexación y chat RAG sobre patrimonio de Canarias**  
> Aplicación web con landing, historial de conversaciones y crawler orientado a generar corpus reutilizable.

[![.NET Framework](https://img.shields.io/badge/.NET-Framework%204.8.1-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-7.3-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![ASP.NET Web Forms](https://img.shields.io/badge/ASP.NET-Web%20Forms-0078D4?logo=microsoft)](https://dotnet.microsoft.com/apps/aspnet)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.2.3-7952B3?logo=bootstrap)](https://getbootstrap.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## Tabla de Contenidos

- [Descripción](#descripción)
- [Stack Tecnológico](#stack-tecnológico)
- [Arquitectura](#arquitectura)
- [Inicio Rápido](#inicio-rápido)
- [Uso y Configuración](#uso-y-configuración)
- [Características Principales](#características-principales)
- [Estructura del Proyecto](#estructura-del-proyecto)

---

## Descripción

**RAGCAN** es una aplicación web ASP.NET Web Forms (.NET Framework 4.8.1) pensada para explorar y consultar un corpus de conocimiento sobre Canarias.

La app combina:

- una **landing page** descriptiva como entrada principal
- una **página de chat RAG** con historial persistente
- un **crawler** para descargar y limpiar contenido web
- una capa de **indexación** para preparar el corpus

El texto limpio extraído se almacena en `App_Data/crawlings/` como archivos `.txt` individuales por página, listos para su uso como corpus en sistemas RAG (Retrieval-Augmented Generation).

**Características clave:**
- Algoritmo **BFS (Breadth-First Search)** para rastreo eficiente
- **Limpieza HTML automática**: elimina scripts, estilos y etiquetas innecesarias
- **Guardado por página**: cada URL se descarga en un fichero `.txt` separado con solo texto limpio
- **Restricción de dominio**: respeta automáticamente los límites del dominio rastreado
- **Control de profundidad**: limita el número de niveles de navegación
- **Indexación de metadatos**: genera `metadata.json` a partir del corpus ya crawleado
- **Interfaz web**: formulario Bootstrap 5 para configurar y lanzar rastreos
- **Chat RAG**: entrada directa al chat, `Enter` para enviar, estado `Pensando` animado y respuestas con fuentes
- **Landing moderna**: portada descriptiva con branding, hero y accesos directos
- **UI responsive unificada**: crawler, chat y master pages usan Bootstrap 5 en escritorio, tablet y móvil
- **Tema oscuro**: con Font Awesome 6.4.0 y persistencia en localStorage
- **Favicon con el logo** de la aplicación

---

## Stack Tecnológico

| Categoría | Tecnología | Versión | Propósito |
|-----------|-----------|---------|----------|
| **Lenguaje** | C# | 7.3 | Código backend y lógica de aplicación |
| **Runtime** | .NET Framework | 4.8.1 | Plataforma de ejecución |
| **Web Framework** | ASP.NET Web Forms | — | Pages, code-behind y controles de servidor |
| **Template Engine** | ASPX | — | Vistas dinámicas (.aspx) con master pages |
| **HTML Parsing** | HtmlAgilityPack | 1.11.61 | DOM parsing y XPath queries |
| **HTTP Client** | System.Net.Http | — | Peticiones HTTP (built-in .NET) |
| **CSS Framework** | Bootstrap | 5.2.3 | Componentes UI y responsive design |
| **Iconos** | Font Awesome | 6.4.0 (CDN) | Iconos UI y toggle de tema oscuro |
| **Serialización** | Newtonsoft.Json | 13.0.3 | Lectura/escritura de metadata.json |
| **Servidor** | IIS Express | — | Desarrollo local |
| **Control de Versión** | Git | — | Repositorio en GitHub |

---

## Arquitectura

### Flujo de Ejecución — Crawling

```
[Usuario] → [Landing.aspx - Entrada principal]
                    ↓
        [Crawler.aspx - Control + estado + logs]
                    ↓
        [Crawler.aspx.cs - BtnIniciar_Click()]
                    ↓
        [CrawlerSettings — validación de parámetros]
                    ↓
        [CrawlerService.CrawlDomain() — BFS Loop]
                    ↓
     [HtmlAgilityPack — ExtraerTextoLimpio()]
                    ↓
     [PathHelper — GuardaFichero en App_Data/crawlings/]
                    ↓
        [Resultados.aspx — resumen por dominio]
```

### Flujo de Ejecución — Indexación

```
[Usuario] → [Indexar.aspx - Selección de carpeta]
                    ↓
        [Indexar.aspx.cs - BtnIndexar_Click()]
                    ↓
        [MetadataService — escanea .txt del corpus]
                    ↓
        [QualityScorer — puntúa cada documento]
                    ↓
        [App_Data/crawlings/.../metadata.json]
```

### Flujo de Ejecución — Chat RAG

```
[Usuario] → [Landing.aspx - Entrada principal]
                    ↓
        [Chat.aspx - Chat RAG]
                    ↓
        [Chat.aspx.cs - Nuevo chat limpio al cargar]
                    ↓
        [RagQueryService - consulta al corpus indexado]
                    ↓
        [Historial de conversaciones + fuentes]
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

### Páginas

#### `Landing.aspx` — Portada principal
- Hero descriptivo con branding de RAGCAN
- Accesos directos a chat y crawler
- Estética oscura alineada con el resto de la app
- Es el documento por defecto de IIS (`Web.config`) y la ruta raíz (`RouteConfig`)

#### `Chat.aspx` — Chat RAG
- `RAG Chat` como cabecera principal
- `RAG` en azul y navegación coherente con la marca
- El formulario de entrada usa un `textarea` que acepta `Enter` para enviar
- Al entrar a la página se inicia un chat nuevo
- Estado `Pensando` animado mientras se prepara la respuesta
- Respuestas con iconos en lugar de emojis y fuentes visibles
- Usa `PageScripts`; tanto `Site.Master` como `Site.Mobile.Master` exponen ese placeholder

#### `Crawler.aspx` — Control del crawler
- Formulario de URL única o archivo `.txt` de semillas
- Selector de archivo a ancho completo para evitar recortes en móvil
- Botones de acción en fila en escritorio y apilados en móvil
- Estado vivo, tabla de fuentes y logs con scroll interno sin romper el ancho de página
- Scheduler integrado para crawl/index programado

#### `Default.aspx` — Compatibilidad antigua
- Conservado por compatibilidad con enlaces previos
- Redirige a `Crawler.aspx`, donde vive la UI actual del crawler

#### `Indexar.aspx` — Generación de metadatos
- `ddlCarpeta` — Dropdown con subcarpetas detectadas automáticamente en `App_Data/`
- `txtCarpetaCustom` — Ruta personalizada relativa a `App_Data/`
- `chkRecursivo` — Escanear subdirectorios recursivamente
- `btnIndexar` — Genera/actualiza `metadata.json`

#### `Resultados.aspx` — Resultados del crawling
- Muestra resumen por dominio: páginas descargadas y ruta de guardado
- Enlace de vuelta al formulario

#### `Contact.aspx` — Página no usada
- Redirige a `Landing.aspx` para evitar caer en la plantilla de contacto por defecto

#### Master Page y Tema Oscuro (`Site.Master`)
- Navbar con navegación y toggle dark mode
- Script pre-paint en `<head>` para evitar parpadeo al cargar
- CSS con custom properties (`--bg-color`, `--text-color`, etc.) en `Content/Site.css`
- Favicon apuntando al logo de la aplicación

#### Master móvil (`Site.Mobile.Master`)
- Replica la estructura responsive de `Site.Master`
- Incluye bundles, `ScriptManager`, navbar, dark mode y `PageScripts`
- Evita errores en páginas que cargan scripts al final, como `Chat.aspx`

---

## Inicio Rápido

### Requisitos Previos
- **Visual Studio 2019+** (Community es suficiente)
- **.NET Framework 4.8.1** SDK (incluido en VS 2019+)
- **IIS Express** (incluido en VS)

### Instalación

1. **Clonar el repositorio**
   ```powershell
   git clone https://github.com/luis-guillen/rag_can_webform.git
   cd rag_can_webform
   ```

2. **Abrir en Visual Studio**
   ```powershell
   explorer rag_can_aspx.slnx
   ```

3. **Restaurar dependencias NuGet**
   - Clic derecho en Solución → "Restore NuGet Packages"
   - O desde Package Manager Console:
     ```powershell
     Update-Package -Reinstall
     ```

4. **Ejecutar localmente**
   - Presionar **F5** (Debug) o **Ctrl+F5** (sin debugger)
   - Se abre automáticamente `https://localhost:<puerto>/`
   - HTTP: 5000 | HTTPS: 44345 (ver `.vs/config/applicationhost.config` para el puerto exacto)

5. **Probar el crawler**
   - Dejar URL vacía para usar seeds de `App_Data/seeds.txt`
   - O introducir una URL válida (ej: `https://ejemplo.com`)
   - Ajustar `maxPages` y `maxDepth`
   - Pulsar "Iniciar crawling"
   - Revisar resultados y ficheros en `App_Data/crawlings/<dominio>/`

---

## Uso y Configuración

### Parámetros del Formulario de Crawling

| Parámetro | Tipo | Rango | Defecto | Descripción |
|-----------|------|-------|---------|------------|
| `url` | text | N/A | vacío | URL a rastrear. Si vacía, se usan seeds de `App_Data/seeds.txt`. |
| `carpeta` | text | N/A | `crawlings/` | Subcarpeta de salida dentro de `App_Data/`. |
| `maxPages` | int | 1–10000 | 50 | Máximo número de páginas a descargar. |
| `maxDepth` | int | 0–10 | 2 | Profundidad máxima de enlaces a seguir. |
| `fullCrawl` | bool | — | false | Permite hasta 1000 páginas. |

### Configuración Común

#### Cambiar carpeta de salida por defecto

En `Services/CrawlerSettings.cs` o `Services/PathHelper.cs`:
```csharp
// Cambiar la subcarpeta base dentro de App_Data:
string carpetaBase = "crawlings";  // → "mi_corpus"
```

#### Cambiar timeout de petición HTTP

En `Services/CrawlerService.cs`:
```csharp
client.Timeout = TimeSpan.FromSeconds(15);  // → 30
```

#### Cambiar delay politeness entre peticiones

En `Services/CrawlerService.cs`:
```csharp
System.Threading.Thread.Sleep(300);  // → 100 (más rápido)
```

#### Cambiar límite de `fullCrawl`

En `Default.aspx.cs`, método `BtnCrawl_Click()`:
```csharp
if (fullCrawl) maxPages = Math.Min(maxPages, 1000);  // → 5000
```

#### Añadir URLs semilla

Editar `App_Data/seeds.txt`, una URL por línea:
```
https://ejemplo.com
https://otro-dominio.org
```

#### Ajustar estilos del tema oscuro

En `Content/Site.css`:
```css
html.dark-mode {
    --bg-color: #121212;
    --text-color: #e0e0e0;
    --navbar-bg: #1e1e1e;
}
```

---

## Características Principales

### Crawling Inteligente
- Algoritmo BFS con control de profundidad
- Restricción automática a dominio único
- Filtro de URLs binarias (`.exe`, `.zip`, `.pdf`, etc.)
- Delay configurable entre peticiones (politeness)
- Detección y evitado de bucles (URLs ya visitadas)

### Limpieza de Contenido
- Eliminación de `<script>`, `<style>`, `<noscript>` via XPath (HtmlAgilityPack)
- Decodificación de entidades HTML (`HtmlEntity.DeEntitize()`)
- Normalización de espacios y saltos de línea
- Guardado en archivos `.txt` puros por página

### Indexación y Metadatos
- Escaneo recursivo de carpetas del corpus
- Puntuación de calidad por documento (`QualityScorer`)
- Detección de duplicados (`DuplicateDetector`)
- Generación de `metadata.json` por lote de crawling

### Interfaz Web
- Formulario Bootstrap 5 responsive
- Tema oscuro persistente (localStorage + cookie fallback)
- Sin parpadeo al cargar (script pre-paint en `<head>`)
- Resumen visual de resultados por dominio
- `Content/Site.css` evita límites globales de `280px` en controles Bootstrap y añade reglas responsive específicas para `Crawler.aspx`
- Tablas y logs del crawler usan scroll interno para no generar overflow horizontal de página

---

## Estructura del Proyecto

```
rag_can_webform/
├── Landing.aspx                   # Portada principal
├── Landing.aspx.cs                # Code-behind de la landing
├── Chat.aspx                      # Chat RAG
├── Chat.aspx.cs                   # Code-behind del chat
├── Chat.aspx.designer.cs
├── Crawler.aspx                    # UI unificada de crawling: control, estado, logs y scheduler
├── Crawler.aspx.cs                 # Code-behind: iniciar/parar crawl, render de estado, fuentes y logs
├── Crawler.aspx.designer.cs
├── Default.aspx                    # Compatibilidad antigua; redirige a Crawler.aspx
├── Default.aspx.cs                 # Code-behind heredado
├── Default.aspx.designer.cs        # Diseñador (autogenerado)
├── Indexar.aspx                    # Formulario para generar metadatos del corpus
├── Indexar.aspx.cs                 # Code-behind: BtnIndexar_Click
├── Indexar.aspx.designer.cs
├── Resultados.aspx                 # Página de resultados del crawling
├── Resultados.aspx.cs
├── Resultados.aspx.designer.cs
├── About.aspx                      # Página informativa
├── Contact.aspx                    # Página no usada; redirige a Landing.aspx
├── Site.Master                     # Master page (layout + navbar + dark mode toggle)
├── Site.Master.cs
├── Site.Master.designer.cs
├── Site.Mobile.Master              # Master móvil alineada con Site.Master + PageScripts
├── Site.Mobile.Master.cs
├── Site.Mobile.Master.designer.cs
├── ViewSwitcher.ascx               # Control de cambio de vista (desktop/móvil)
├── ViewSwitcher.ascx.cs
├── Services/
│   ├── ChatHistoryService.cs      # Historial persistente de conversaciones
│   ├── CrawlerService.cs           # Motor BFS: descarga y extracción de texto
│   ├── CrawlerSettings.cs          # Validación y encapsulación de parámetros
│   ├── CrawlJobManager.cs          # Gestión del estado del trabajo
│   ├── DuplicateDetector.cs        # Detección de URLs duplicadas
│   ├── MetadataService.cs          # Generación de metadata.json
│   ├── PathHelper.cs               # Construcción de rutas en App_Data
│   ├── QualityScorer.cs            # Puntuación de calidad de documentos
│   ├── RagQueryService.cs          # Consulta al backend RAG y health check
│   └── SeedUrlProvider.cs          # Proveedor de URLs semilla
├── App_Data/
│   ├── seeds.txt                   # URLs semilla (una por línea)
│   └── crawlings/                  # Salida de crawlings (generada en runtime)
│       └── <dominio>/
│           ├── 00_index.txt
│           ├── 01_about.txt
│           └── metadata.json       # Generado por Indexar.aspx
├── Content/
│   ├── bootstrap.css               # Bootstrap 5.2.3
│   └── Site.css                    # Estilos personalizados + dark mode tokens
├── Scripts/
│   ├── bootstrap.bundle.js         # Bootstrap + Popper
│   ├── jquery-3.7.0.min.js
│   ├── modernizr-2.8.3.js
│   └── WebForms/                   # Scripts del framework ASP.NET Web Forms
│       └── MSAjax/                 # Microsoft AJAX
├── App_Start/
│   ├── BundleConfig.cs             # Bundling de CSS/JS
│   └── RouteConfig.cs              # Rutas amigables (home → Landing.aspx, /Crawler → Crawler.aspx)
├── Properties/
│   └── AssemblyInfo.cs
├── Global.asax                     # Configuración global de la aplicación
├── Global.asax.cs
├── Web.config                      # Configuración ASP.NET e IIS
├── Web.Debug.config
├── Web.Release.config
├── Bundle.config
├── packages.config                 # Dependencias NuGet
├── rag_can_aspx.csproj             # Proyecto C#
└── README.md
```

---

## Dependencias NuGet

| Paquete | Versión | Uso |
|---------|---------|-----|
| Bootstrap | 5.2.3 | UI framework |
| jQuery | 3.7.0 | DOM (requerido por WebForms) |
| HtmlAgilityPack | 1.11.61 | DOM parsing y extracción de texto |
| Newtonsoft.Json | 13.0.3 | Serialización de metadata.json |
| Microsoft.AspNet.FriendlyUrls | 1.0.2 | URLs amigables en Web Forms |
| Microsoft.AspNet.Web.Optimization | 1.1.3 | Bundling y minificación CSS/JS |
| Microsoft.AspNet.ScriptManager.WebForms | 5.0.0 | Script Manager para Web Forms |
| Microsoft.CodeDom.Providers.DotNetCompilerPlatform | 2.0.1 | Compilador Roslyn |
| Modernizr | 2.8.3 | Detección de características del navegador |

---

## Operación en background (NIVEL 1): crawler/indexer controlables

A partir de esta versión, el crawler y el indexer funcionan como **procesos en segundo plano
controlables** desde Web Forms (iniciar / parar / consultar progreso / ver logs), con estado
**persistido en disco**, **crawling e indexado incrementales por hash**, un **scheduler interno**
y exposición opcional como **servicio WCF**. No se ejecuta trabajo largo dentro del request web.

### Arquitectura nueva

```
UI:  Crawler.aspx        Indexar.aspx       (Default.aspx / Resultados.aspx -> redirigen a Crawler.aspx)
        |                     |
        v                     v
   ┌──────────────────────────────────────────┐       ┌──────────────────────────────┐
   │  CrawlerIndexerFacade  (capa de control)  │ <──── │ CrawlerIndexerService.svc     │
   │  StartCrawl/StopCrawl/GetCrawlStatus/...   │  WCF  │ + ICrawlerIndexerService      │
   └──────────────────────────────────────────┘       └──────────────────────────────┘
        |            |              |             |
        v            v              v             v
   CrawlJob      IndexJob      JobStatusManager   Scheduler (Timer in-process)
        |            |              |
        v            v              v
   CrawlerService MetadataService  App_Data/status/*.json  +  App_Data/logs/*.log  (escritura atomica)
   (reutilizados)
```

Cada job se lanza con `HostingEnvironment.QueueBackgroundWorkItem`, escribe su estado mediante
`JobStatusManager`, respeta un `CancellationToken` (para *Parar*) y un *single-flight lock*
(impide ejecuciones concurrentes duplicadas). El estado persiste a disco, por lo que sobrevive a
que el usuario cambie de página o recargue.

### Métodos públicos (fachada y WCF)

Definidos en `Services/CrawlerIndexerFacade.cs` y expuestos por `Services/Wcf/ICrawlerIndexerService.cs`:

| Método | Descripción |
|--------|-------------|
| `StartCrawl()` | Lanza el crawl de todas las semillas de `seeds.txt` en segundo plano. |
| `StartCrawlSource(string url)` | Crawl de una sola URL. |
| `StopCrawl()` | Solicita parar el crawl en curso. |
| `GetCrawlStatus()` | Estado actual del crawl (estado, progreso, URL actual, contadores). |
| `GetLastCrawlRun()` | Última ejecución del crawl (mismo fichero de estado persistido). |
| `StartIndexing()` | Indexa en segundo plano solo lo que tiene `needs_index=true`. |
| `StopIndexing()` | Solicita parar la indexación. |
| `GetIndexingStatus()` / `GetLastIndexingRun()` | Estado de la indexación. |
| `GetSources()` | Lista de fuentes con su estado. |
| `GetSourceStatus(string url)` | Estado de una fuente concreta. |
| `GetLogs(int lines)` | Últimas N líneas de `crawler.log` e `indexer.log`. |

### Ficheros de estado y logs (`App_Data/`)

Se crean automáticamente al arrancar la app (`Global.asax` → `JobStatusManager.EnsureFolders()`):

| Fichero | Contenido |
|---------|-----------|
| `status/crawl_status.json` | Estado del crawl: `state` (idle/running/completed/error/stopped), `started_at`, `finished_at`, `total_sources`, `processed_sources`, `failed_sources`, `skipped_sources`, `current_url`, `last_error`, `progress_percent`. |
| `status/index_status.json` | Igual que el anterior, para la indexación. |
| `status/sources_status.json` | Una entrada por URL: `last_crawled_at`, `http_status`, `title`, `content_sha256`, rutas (`txt_path`/`metadata_path`), `needs_index`, `last_indexed_at`, `chunk_count`, `pages_total/changed/skipped`, `state`, `last_error`. |
| `status/scheduler_config.json` | Configuración del scheduler (modo, intervalo/hora, habilitado). |
| `logs/crawler.log`, `logs/indexer.log` | Logs con timestamp UTC (rotan a `.1` al superar ~5 MB). |

> Se mantiene la compatibilidad con `App_Data/crawlings/` y `App_Data/seeds.txt`. Los `.txt` y los
> sidecars `*.metadata.json` siguen generándose igual; ahora el sidecar incluye además
> `needs_index`, `last_indexed_at` y `chunks`.

### Cómo lanzar el crawling

1. Ir a **Crawler.aspx** (enlace *Crawler* del menú).
2. Opcional: indicar una URL única, ajustar *Max Páginas* / *Max Profundidad*.
3. Pulsar **Iniciar Crawling**. El job arranca en segundo plano y la página muestra estado,
   barra de progreso, URL actual, contadores, tabla de fuentes y logs (refresco automático cada 3 s).
4. Se puede cerrar o cambiar de página: el job sigue. Para detenerlo, **Parar**.

**Incremental por hash:** para cada página se calcula el SHA-256 del texto limpio. Si coincide con el
del crawl anterior, se marca como *skipped* y **no** se vuelve a indexar; si cambió (o es nueva), se
marca `needs_index=true`.

### Cómo lanzar la indexación

1. Ir a **Indexar.aspx**.
2. Pulsar **Iniciar Indexado**: procesa **solo** los documentos con `needs_index=true`, calcula el
   número de *chunks*, registra `last_indexed_at` y pone `needs_index=false`.
3. (Conservado) *Regenerar metadata (manual)*: escanea una carpeta y regenera `metadata.json`
   + sidecars sin volver a crawlear.

> **Integración Qdrant (preparada, no activa):** el push real de *embeddings* a Qdrant
> (`rag_can_python`) está abstraído en `Services/Jobs/IVectorIndexSink.cs`. Por defecto se usa
> `NullVectorIndexSink` (no-op). Para activarlo en el futuro, implementar `RagPythonVectorIndexSink`
> y asignarlo a `IndexJob.Sink`.

### Cómo programarlo (scheduler)

En **Crawler.aspx**, tarjeta *Programación*:
- **Modo**: `manual` (sin programación), `interval` (cada X horas) o `daily` (diario a una hora).
- Marcar *Ejecutar crawl programado* y/o *Ejecutar indexado tras el crawl*.
- **Guardar programación** → se persiste en `App_Data/status/scheduler_config.json`.

Un `Timer` interno (arrancado en `Global.asax` → `Scheduler.Start()`) revisa la configuración cada
minuto y, si toca y no hay jobs en curso, ejecuta el ciclo **crawl → index**.

> **Importante (in-process):** el scheduler interno solo se ejecuta mientras el *app pool* esté vivo.
> En IIS conviene habilitar **Application Initialization** / *AlwaysRunning* (o un *keep-alive*) para
> que no se duerma. Ver más abajo la alternativa con Tarea programada de Windows.

### Servicio WCF (opcional)

- Endpoint: `/Services/Wcf/CrawlerIndexerService.svc` (SOAP / `basicHttpBinding`, con WSDL).
- Implementación delgada que delega en `CrawlerIndexerFacade`.
- **Requisito**: tener instalada la característica de Windows **"WCF HTTP Activation"** (en
  *Activar o desactivar características de Windows → .NET Framework 4.8 Advanced Services →
  Activación de WCF → Activación HTTP*) para que IIS/IIS Express mapeen la extensión `.svc`.
- Probar con el *WCF Test Client* (`WcfTestClient.exe`) o `Add Service Reference` apuntando al `.svc`.
- Si la característica no está instalada, la web y la UI siguen funcionando con normalidad; solo
  queda inaccesible el endpoint WCF (la fachada interna es la vía principal).

### Cómo escalar de 5 a 500 URLs

1. **Ampliar las semillas**: añadir las ~500 URLs a `App_Data/seeds.txt` (o `~/Config/seeds.txt`), una por línea.
2. **Ajustar concurrencia/politeness** en `Web.config`:
   - `Crawler:MaxConcurrentDomains` (subir con cuidado, p. ej. 4–8),
   - `Crawler:RequestDelayMs`, `Crawler:HttpTimeoutSeconds`,
   - `Crawler:MaxPages` / `Crawler:MaxDepth`, `Index:ChunkSize`.
3. **Incremental**: en cada ejecución solo se re-indexa lo que cambió (hash), por lo que el coste
   de mantener 500 webs al día es bajo.
4. **Programación robusta para producción**: en lugar del scheduler in-process, usar la **Tarea
   programada de Windows** (ver abajo) que sobrevive a reciclajes del *app pool*.
5. **Vectorización real**: conectar `IVectorIndexSink` con `rag_can_python`/Qdrant para indexar a escala.

### Ejecutar en Windows (PC propio y servidor del profesor)

> El proyecto es **.NET Framework 4.8.1 + ASP.NET Web Forms**: se compila y ejecuta en **Windows**
> (Visual Studio 2022 / `msbuild` / IIS Express / IIS). No se ejecuta en Linux/WSL.

**En tu PC (desarrollo):**
1. Abrir la solución en Visual Studio 2022 y restaurar paquetes NuGet.
2. Compilar (F6) y ejecutar (F5 / Ctrl+F5) con IIS Express.
3. Asegurar permisos de escritura en `App_Data/` (normalmente automático con IIS Express).

**En el servidor Windows del profesor (IIS):**
1. Publicar el sitio (Build → Publish, o copiar el contenido compilado) a una carpeta del servidor.
2. Crear un sitio/aplicación en IIS apuntando a esa carpeta, con un *Application Pool* de
   **.NET Framework v4.0** (modo integrado).
3. Dar permisos de **escritura** a la identidad del *app pool* (p. ej. `IIS AppPool\<nombre>`) sobre
   `App_Data/` (subcarpetas `status`, `logs`, `crawlings`).
4. (Recomendado) Habilitar **Application Initialization** y poner el *app pool* en `AlwaysRunning`
   para que el scheduler interno no se detenga.
5. (Opcional WCF) Instalar **WCF HTTP Activation**.

**Programación con Tarea de Windows (alternativa de producción, sobrevive a reciclajes):**
- Crear una tarea en el *Programador de tareas* que, en el horario deseado, invoque el servicio
  (p. ej. con `curl`/PowerShell) llamando a `StartCrawl` y `StartIndexing` del `.svc`, o a una URL
  de disparo de la aplicación. Así el ciclo no depende de que el *app pool* esté activo en ese momento.

### Robustez

- *Single-flight*: no se permiten dos crawls (ni dos indexados) simultáneos.
- Control de excepciones **por URL**: una URL que falla no detiene el resto (se registra en
  `failed_sources` y en el log).
- Toda escritura queda **anclada a `App_Data`** (validación de rutas en `PathHelper`).
- *Parar* cancela el job vía `CancellationToken` y deja el estado en `stopped`.
- Tras un reciclaje del *app pool*, los estados que quedaron en `running` se reparan a `stopped`
  al arrancar (`JobStatusManager.ReconcileOnStartup()`).

### Nuevos archivos relevantes

```
Services/CrawlerIndexerFacade.cs        # capa de control (metodos publicos)
Services/Jobs/JobStatusModels.cs        # modelos de estado (JSON)
Services/Jobs/JobStatusManager.cs       # estado central + single-flight + cancelacion
Services/Jobs/JsonFile.cs               # escritura/lectura JSON atomica
Services/Jobs/JobLogger.cs              # logs con rotacion
Services/Jobs/CrawlJob.cs               # crawl incremental
Services/Jobs/IndexJob.cs               # indexado incremental
Services/Jobs/Chunker.cs               # troceo (chunk_count + base para Qdrant)
Services/Jobs/IVectorIndexSink.cs       # hook Qdrant (NullVectorIndexSink por defecto)
Services/Jobs/Scheduler.cs              # scheduler in-process
Services/Wcf/ICrawlerIndexerService.cs  # contrato WCF + DTOs
Services/Wcf/CrawlerIndexerService.svc(.cs)  # servicio WCF (wrapper de la fachada)
Crawler.aspx(.cs/.designer.cs)          # UI unificada de crawling
```

---

## Licencia

Este proyecto está bajo licencia **MIT**. Consulta `LICENSE` para más detalles.

---

**Última actualización:** 2026-06-08 | **Versión:** 1.1 | **Estado:** En desarrollo
