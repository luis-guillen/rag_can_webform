# 🕷️ RAG Canarias

> **Proyecto ASP.NET Web Forms para crawling web y limpieza de contenido HTML**  
> Trabajo de Fin de Grado — Aplicación para descargar y procesar páginas web de un dominio de forma automática.

[![.NET Framework](https://img.shields.io/badge/.NET-Framework%204.8.1-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-7.3-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![ASP.NET Web Forms](https://img.shields.io/badge/ASP.NET-Web%20Forms-0078D4?logo=microsoft)](https://dotnet.microsoft.com/apps/aspnet)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.2.3-7952B3?logo=bootstrap)](https://getbootstrap.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## 📋 Tabla de Contenidos

- [Descripción](#-descripción)
- [Stack Tecnológico](#-stack-tecnológico)
- [Arquitectura](#-arquitectura)
- [Inicio Rápido](#-inicio-rápido)
- [Uso y Configuración](#-uso-y-configuración)
- [Características Principales](#-características-principales)
- [Estructura del Proyecto](#-estructura-del-proyecto)
---

## 📖 Descripción

**RAG Canarias** es una aplicación web que implementa un **crawler web con control de profundidad** para descargar y procesar automáticamente todas las páginas de un dominio. 

**Características clave:**
- 🔄 Algoritmo **BFS (Breadth-First Search)** para rastreo eficiente
- 🧹 **Limpieza HTML automática**: elimina scripts, estilos y etiquetas innecesarias
- 📄 **Guardado por página**: cada URL se descarga en un fichero `.txt` separado con solo texto limpio
- 🌐 **Restricción de dominio**: respeta automáticamente los límites del dominio
- 🕐 **Control de profundidad**: limita el número de niveles de navegación
- 📊 **Interfaz web intuitiva**: formulario para configurar parámetros de rastreo
- 🌙 **Tema oscuro integrado**: con Font Awesome y persistencia en localStorage

---

## 🛠️ Stack Tecnológico

| Categoría | Tecnología | Versión | Propósito |
|-----------|-----------|---------|----------|
| **Lenguaje** | C# | 7.3 | Código backend y lógica de aplicación |
| **Runtime** | .NET Framework | 4.8.1 | Plataforma de ejecución |
| **Web Framework** | ASP.NET Web Forms | - | Pages, code-behind y controles servidor |
| **Template Engine** | ASPX | - | Vistas dinámicas (.aspx) con master pages |
| **HTML Parsing** | HtmlAgilityPack | Latest (NuGet) | DOM parsing y XPath queries |
| **HTTP Client** | System.Net.Http | - | Peticiones HTTP (built-in .NET) |
| **CSS Framework** | Bootstrap | 5.2.3 | Componentes UI y responsive design |
| **Iconos** | Font Awesome | 6.4.0 (CDN) | Iconos del toggle de tema oscuro |
| **Servidor** | IIS Express | - | Desarrollo local |
| **Control de Versión** | Git | - | Repositorio en GitHub |

---

## 🏗️ Arquitectura

### Flujo de Ejecución

```
[Usuario] → [Formulario Index.aspx] → [Page_Load() / Button_Click() en Index.aspx.cs]
                                            ↓
                                   [Validación de parámetros]
                                            ↓
                                   [CrawlDomain() - BFS Loop]
                                            ↓
                             [Descarga página + ExtraerTextoLimpio()]
                                            ↓
                             [GuardaFichero.txt en C:\temp\crawler\]
                                            ↓
                                   [Resultados.aspx]
```

### Componentes Principales

#### 1. **Page: `Index.aspx` y `Index.aspx.cs`**
- **`Index.aspx`** — Formulario de entrada con controles ASP.NET:
  - TextBox para URL (opcional — si vacía usa seeds por defecto)
  - TextBox para `maxPages` (rango: 1–10000, defecto: 50)
  - TextBox para `maxDepth` (rango: 0–10, defecto: 2)
  - CheckBox para `fullCrawl` (permite hasta 1000 páginas)
  - Button "Iniciar crawling" (PostBack)

- **`Index.aspx.cs`** (Code-behind) — Lógica del servidor:
  - **`BtnCrawl_Click()`** — Manejador de evento del botón (HttpPost implícito)
    - Valida parámetros (`url`, `maxPages`, `maxDepth`, `fullCrawl`)
    - Soporta múltiples seeds por defecto
    - Aplica límite seguro: `fullCrawl` máximo **1000 páginas**

  - **`CrawlDomain()`** — Motor BFS
    - `Queue<Tuple<Uri, int>>` con profundidad integrada
    - Respeta `maxDepth`: no expande enlaces cuando `depth >= maxDepth`
    - HttpClient con timeout de **15 segundos**
    - Delay politeness de **300ms** entre peticiones
    - Detección y filtro de URLs no rastreables (extensiones binarias, etc.)

  - **`ExtraerTextoLimpio()`** — Limpieza HTML
    - XPath: `//script | //style | //noscript` → eliminadas (requiere HtmlAgilityPack)
    - Decodificación de entidades HTML (`DeEntitize()`)
    - Trim de líneas vacías y normalización de espacios

  - **`ExtraerEnlacesInternos()`** — Extracción de URLs
    - XPath: `//a[@href]` para todos los links
    - Validación: solo `http://` y `https://`
    - Restricción de dominio: `Uri.Host` debe coincidir
    - Normalización de rutas relativas (`Uri.TryCreate()`)

#### 2. **Pages:**

- **`Resultados.aspx`** — Página de resultados
  - Resumen de ejecución después del crawling
  - Lista cada dominio rastreado con cantidad de páginas y ruta de guardado
  - Formato: `"ejemplo.com → 47 páginas guardadas en C:\temp\crawler\ejemplo_com\"`

#### 3. **Master Page y Tema Oscuro (`Site.Master` + `Content/Site.css`)**

- **`Site.Master`** — Master page principal:
  - Navbar con título y navegación
  - Toggle button para dark mode (Font Awesome 6.4.0)
  - ContentPlaceHolder para el contenido de las páginas

- **Script en `Site.Master` `<head>`** (ejecución pre-paint):
  ```javascript
  // Lee localStorage/cookie ANTES de primer render
  // Aplica clase 'dark-mode' a <html>
  // Evita flicker claro→oscuro
  ```

- **Diseño de tokens CSS** (`Content/Site.css`):
  - Variables CSS custom: `--bg-color`, `--text-color`, `--input-bg`, etc.
  - `html.dark-mode { ... }` redefine todas las variables
  - Overrides Bootstrap con especificidad alta + `!important`
  - Transiciones suaves (0.15s) en colores

- **Toggle Button**:
  - Botón en navbar
  - Icono Font Awesome: `fa-moon` (oscuro) ↔ `fa-sun` (claro)
  - Evento click → toggle clase `dark-mode` en `html`
  - Persistencia: `localStorage.setItem('darkMode', isDark)` + cookie fallback

---

## 🚀 Inicio Rápido

### Requisitos Previos
- **Visual Studio 2019+** (Community es suficiente)
- **.NET Framework 4.8.1** (incluido en VS 2019+)
- **IIS Express** (incluido en VS)
- **Acceso de escritura** en `C:\temp\` (o carpeta alternativa configurada)

### Pasos de Instalación

1. **Clonar el repositorio**
   ```powershell
   git clone https://github.com/luis-guillen/rag_canarias_win.git
   cd rag_canarias
   ```

2. **Abrir en Visual Studio**
   ```powershell
   # Desde el directorio del proyecto
   explorer rag_canarias.sln
   ```

3. **Restaurar dependencias NuGet**
   - Clic derecho en Solución → "Restore NuGet Packages"
   - O desde Package Manager Console:
     ```powershell
     Update-Package -Reinstall
     ```

4. **Ejecutar localmente**
   - Presionar **F5** (Debug) o **Ctrl+F5** (Sin debug)
   - Se abre automáticamente `https://localhost:<puerto>/Home/Index`

5. **Probar el crawler**
   - Dejar URL vacía para usar seeds por defecto (ej: `www.ejemplo.com`)
   - O introducir una URL válida (ej: `https://ejemplo.com`)
   - Ajustar `maxPages` (defecto 50) y `maxDepth` (defecto 2)
   - Pulsar "Iniciar crawling"
   - Revisar resultados y ficheros en `C:\temp\crawler\<dominio>\`

---

## ⚙️ Uso y Configuración

### Parámetros del Formulario

| Parámetro | Tipo | Rango | Defecto | Descripción |
|-----------|------|-------|---------|------------|
| `url` | text | N/A | vacío | URL a rastrear. Si está vacía, se usan seeds por defecto. |
| `maxPages` | int | 1–10000 | 50 | Máximo número de páginas a descargar del dominio. |
| `maxDepth` | int | 0–10 | 2 | Profundidad máxima de enlaces a seguir desde la página inicial. |
| `fullCrawl` | bool | true/false | false | Si se marca, permite hasta 1000 páginas (sin límite normal). |

### Cambios de Configuración Comunes

#### ✏️ Cambiar la carpeta de salida

En `Index.aspx.cs` (método `CrawlDomain()` o similar):
```csharp
// Antes:
string carpetaBaseGlobal = @"C:\temp\crawler\";

// Después (ejemplo):
string carpetaBaseGlobal = @"D:\misCrawls\";
```
✅ **Asegúrate** de que la cuenta que ejecuta IIS Express tiene permisos de escritura en esa ruta.

#### ✏️ Cambiar timeout de petición HTTP

En `Index.aspx.cs`, método `CrawlDomain()`:
```csharp
// Antes:
using (var client = new HttpClient())
{
    client.Timeout = TimeSpan.FromSeconds(15);
    // ...
}

// Después (más tolerante):
client.Timeout = TimeSpan.FromSeconds(30);
```

#### ✏️ Cambiar delay politeness entre peticiones

En `Index.aspx.cs`, método `CrawlDomain()`:
```csharp
// Antes:
System.Threading.Thread.Sleep(300); // 300ms entre peticiones

// Después (más rápido, pero menos amigable):
System.Threading.Thread.Sleep(100); // 100ms
```

#### ✏️ Cambiar límite de `fullCrawl`

En `Index.aspx.cs`, método `BtnCrawl_Click()`:
```csharp
// Antes:
if (fullCrawl) maxPages = Math.Min(maxPages, 1000);

// Después (permitir más):
if (fullCrawl) maxPages = Math.Min(maxPages, 5000);
```

#### ✏️ Ajustar estilos oscuros

En `Content/Site.css`, buscar sección "Design tokens – dark":
```css
html.dark-mode {
    --bg-color: #121212;      /* Cambiar color de fondo */
    --text-color: #e0e0e0;    /* Cambiar color de texto */
    --navbar-bg: #1e1e1e;     /* Cambiar color navbar */
    /* ... más variables ... */
}
```

---

## ✨ Características Principales

### 🔄 Crawling Inteligente
- ✅ Algoritmo BFS con control de profundidad
- ✅ Restricción automática a dominio único
- ✅ Filtro de URL binarias (`.exe`, `.zip`, etc.)
- ✅ Delay configurables entre peticiones (politeness)
- ✅ Detección y evitar loops infinitos

### 📄 Limpieza de Contenido
- ✅ Eliminación automática de `<script>`, `<style>`, `<noscript>`
- ✅ Decodificación de entidades HTML
- ✅ Normalización de espacios y saltos de línea
- ✅ Guardado en ficheros `.txt` puros (sin formato)

### 🖥️ Interfaz Intuitiva
- ✅ Formulario web Bootstrap 5
- ✅ Validación cliente y servidor
- ✅ Resumen visual de resultados
- ✅ Indicación de progreso (parámetros aplicados)

### 🌙 Tema Oscuro
- ✅ Font Awesome 6.4.0 CDN
- ✅ Toggle persistente (localStorage + cookie)
- ✅ Sin parpadeos al cargar (script pre-paint)
- ✅ Overrides Bootstrap profesionales
- ✅ Transiciones suaves (0.15s)

---

## 📁 Estructura del Proyecto

```
rag_canarias/
├── Index.aspx                      # Página de entrada (formulario)
├── Index.aspx.cs                   # Code-behind (lógica crawler: Crawl, CrawlDomain, etc.)
├── Index.aspx.designer.cs          # Diseñador (autogenerado)
├── Resultados.aspx                 # Página de resultados
├── Resultados.aspx.cs              # Code-behind de resultados
├── Site.Master                     # Master page (layout principal + navbar + toggle oscuro)
├── Site.Master.cs                  # Code-behind del master
├── Site.Master.designer.cs         # Diseñador (autogenerado)
├── Site.Mobile.Master              # Master page para móvil
├── Content/
│   ├── bootstrap.css               # Bootstrap 5.2.3
│   └── Site.css                    # Estilos personalizados + dark-mode
├── Scripts/                        # Archivos JavaScript
│   └── darkmode.js                 # Toggle de tema oscuro (opcional)
├── App_Start/
│   ├── BundleConfig.cs             # Bundling de CSS/JS (bootstrap → site.css)
│   └── RouteConfig.cs              # Configuración de rutas amigables
├── Global.asax                     # Configuración global de aplicación
├── Web.config                      # Configuración ASP.NET e IIS
├── Web.Debug.config                # Configuración específica de Debug
├── Web.Release.config              # Configuración específica de Release
├── packages.config                 # Dependencias NuGet
├── rag_can_aspx.csproj             # Proyecto C#
└── README.md                       # Este archivo

# Estructura de salida (generada durante crawling):
C:\temp\crawler\
└── ejemplo_com/                    # Carpeta por dominio
    ├── 00_index.txt
    ├── 01_about.txt
    ├── 02_products.txt
    └── ...
```

---


## 📄 Licencia

Este proyecto está bajo licencia **MIT**. Consulta `LICENSE` para más detalles.

---

**Última actualización:** 2026 | **Versión:** 1.0 | **Status:** ✅ Completo
