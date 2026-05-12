# RAG Canarias

> **Proyecto ASP.NET Web Forms para crawling web y limpieza de contenido HTML**  
> Trabajo de Fin de Grado — Aplicación para descargar y procesar páginas web de un dominio de forma automática.

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

**RAG Canarias** es una aplicación web ASP.NET Web Forms (.NET Framework 4.8.1) que implementa un **crawler web con control de profundidad** para descargar y procesar automáticamente páginas de uno o varios dominios.

El texto limpio extraído se almacena en `App_Data/crawlings/` como archivos `.txt` individuales por página, listos para su uso como corpus en sistemas RAG (Retrieval-Augmented Generation).

**Características clave:**
- Algoritmo **BFS (Breadth-First Search)** para rastreo eficiente
- **Limpieza HTML automática**: elimina scripts, estilos y etiquetas innecesarias
- **Guardado por página**: cada URL se descarga en un fichero `.txt` separado con solo texto limpio
- **Restricción de dominio**: respeta automáticamente los límites del dominio rastreado
- **Control de profundidad**: limita el número de niveles de navegación
- **Indexación de metadatos**: genera `metadata.json` a partir del corpus ya crawleado
- **Interfaz web**: formulario Bootstrap 5 para configurar y lanzar rastreos
- **Tema oscuro**: con Font Awesome 6.4.0 y persistencia en localStorage

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
[Usuario] → [Default.aspx - Formulario]
                    ↓
        [Default.aspx.cs - BtnCrawl_Click()]
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

### Capa de Servicios (`Services/`)

| Clase | Responsabilidad |
|-------|----------------|
| `CrawlerService` | Motor BFS: descarga, extrae texto, sigue enlaces |
| `CrawlerSettings` | Validación y encapsulación de parámetros del formulario |
| `CrawlJobManager` | Gestión del estado del trabajo de crawling |
| `DuplicateDetector` | Evita procesar URLs duplicadas o ya visitadas |
| `MetadataService` | Genera y actualiza `metadata.json` desde el corpus |
| `QualityScorer` | Puntúa documentos por calidad de texto |
| `SeedUrlProvider` | Lee y provee las URLs semilla desde `App_Data/seeds.txt` |
| `PathHelper` | Centraliza la construcción de rutas dentro de `App_Data/` |

### Páginas

#### `Default.aspx` — Formulario de crawling
Controles ASP.NET:
- `txtUrl` — URL a rastrear (opcional; si vacía usa `seeds.txt`)
- `txtCarpeta` — Subcarpeta de salida dentro de `App_Data/` (defecto: `crawlings/`)
- `txtMaxPages` — Límite de páginas (1–10000, defecto: 50)
- `txtMaxDepth` — Profundidad máxima (0–10, defecto: 2)
- `chkFullCrawl` — Permite hasta 1000 páginas
- `btnCrawl` — Inicia el crawling (PostBack)

#### `Indexar.aspx` — Generación de metadatos
- `ddlCarpeta` — Dropdown con subcarpetas detectadas automáticamente en `App_Data/`
- `txtCarpetaCustom` — Ruta personalizada relativa a `App_Data/`
- `chkRecursivo` — Escanear subdirectorios recursivamente
- `btnIndexar` — Genera/actualiza `metadata.json`

#### `Resultados.aspx` — Resultados del crawling
- Muestra resumen por dominio: páginas descargadas y ruta de guardado
- Enlace de vuelta al formulario

#### Master Page y Tema Oscuro (`Site.Master`)
- Navbar con navegación y toggle dark mode
- Script pre-paint en `<head>` para evitar parpadeo al cargar
- CSS con custom properties (`--bg-color`, `--text-color`, etc.) en `Content/Site.css`

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

---

## Estructura del Proyecto

```
rag_can_webform/
├── Default.aspx                    # Formulario principal de crawling
├── Default.aspx.cs                 # Code-behind: BtnCrawl_Click, lógica de inicio
├── Default.aspx.designer.cs        # Diseñador (autogenerado)
├── Indexar.aspx                    # Formulario para generar metadatos del corpus
├── Indexar.aspx.cs                 # Code-behind: BtnIndexar_Click
├── Indexar.aspx.designer.cs
├── Resultados.aspx                 # Página de resultados del crawling
├── Resultados.aspx.cs
├── Resultados.aspx.designer.cs
├── About.aspx                      # Página informativa
├── Contact.aspx                    # Página de contacto
├── Site.Master                     # Master page (layout + navbar + dark mode toggle)
├── Site.Master.cs
├── Site.Master.designer.cs
├── Site.Mobile.Master              # Master page para dispositivos móviles
├── Site.Mobile.Master.cs
├── Site.Mobile.Master.designer.cs
├── ViewSwitcher.ascx               # Control de cambio de vista (desktop/móvil)
├── ViewSwitcher.ascx.cs
├── Services/
│   ├── CrawlerService.cs           # Motor BFS: descarga y extracción de texto
│   ├── CrawlerSettings.cs          # Validación y encapsulación de parámetros
│   ├── CrawlJobManager.cs          # Gestión del estado del trabajo
│   ├── DuplicateDetector.cs        # Detección de URLs duplicadas
│   ├── MetadataService.cs          # Generación de metadata.json
│   ├── PathHelper.cs               # Construcción de rutas en App_Data
│   ├── QualityScorer.cs            # Puntuación de calidad de documentos
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
│   └── RouteConfig.cs              # Rutas amigables (FriendlyUrls)
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

## Licencia

Este proyecto está bajo licencia **MIT**. Consulta `LICENSE` para más detalles.

---

**Última actualización:** 2026 | **Versión:** 1.0 | **Estado:** En desarrollo
