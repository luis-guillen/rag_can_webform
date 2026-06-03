using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace rag_can_aspx.Services
{
    /// <summary>
    /// Servicio de crawling independiente de MVC/Web Forms.
    /// Reutilizable en cualquier proyecto .NET Framework 4.8.1+
    /// </summary>
    public class CrawlerService
    {
        private readonly int _requestDelayMs;
        private readonly TimeSpan _httpTimeout;
        private readonly MetadataService _metadataService;

        private static readonly string[] _nodosBasura =
        {
            "script", "style", "noscript", "nav", "header", "footer", "aside"
        };

        private static readonly string[] _nodosBasuraFallback =
        {
            "script", "style", "noscript", "nav", "header", "footer", "aside", "form"
        };

        private static readonly string[] _nodosInteractivos =
        {
            "input", "button", "select", "textarea", "option"
        };

        private static readonly string[] _nodosUtiles =
        {
            "h1", "h2", "h3", "h4", "h5", "h6", "p", "li", "blockquote", "figcaption", "td", "th", "dt", "dd"
        };

        private static readonly string[] _tokensRuidoAtributos =
        {
            "cookie", "cookies", "consent", "privacidad", "privacy",
            "accesibilidad", "accessibility", "newsletter", "suscripcion",
            "suscribe", "buscar", "buscador", "search", "breadcrumb",
            "share", "social", "menu", "nav", "footer", "header",
            "sidebar", "popup", "modal", "banner"
        };

        private static readonly string[] _patronesBoilerplate =
        {
            "nuestra oficina se encuentra en la primera planta de",
            "la casa amarilla",
            "memoria@cabildodelanzarote.com",
            "e-mail:",
            "email:",
            "islas canarias, españa"
        };

        private static readonly string[] _tokensRutasBajoValor =
        {
            "politica-privacidad", "politica-cookies", "cookies", "privacidad",
            "busqueda-avanzada", "contacto", "nosotros"
        };

        private static readonly string[] _camposMetadataPrioritarios =
        {
            "descripción", "autor", "propietario", "periodo", "fecha",
            "tipo de fotografía", "ámbito geográfico", "soporte",
            "medidas", "referencia"
        };

        private static readonly string[] _camposMetadataDescartables =
        {
            "aviso legal"
        };

        private static readonly string[] _patronesRuido =
        {
            "aviso legal", "política de privacidad", "política de cookies", "uso de cookies",
            "contacto", "teléfono", "correo electrónico", "compartir", "enviar comentario",
            "suscríbete", "síguenos", "redes sociales", "todos los derechos reservados",
            "copyright", "newsletter", "iniciar sesión", "cerrar sesión", "registrar",
            "politica de privacidad", "politica de cookies",
            "telefono", "correo electronico",
            "suscribete", "siguenos",
            "iniciar sesion", "cerrar sesion",
            "optimizacion de la navegacion con el teclado",
            "perfil de navegacion del teclado",
            "google analytics", "configuracion de cookies",
            "aceptar cookies", "rechazar cookies"
        };

        private const string HostCanariasAzul = "canarias-azul.iatext.ulpgc.es";
        private const string EventTargetCerrarDetalleCanariasAzul = "ctl00$MainContent$BotonCerrarDatosElemento";

        public CrawlerService()
            : this(CrawlerSettings.Load(), null)
        {
        }

        public CrawlerService(CrawlerSettings settings, string projectRoot = null)
        {
            settings = settings ?? CrawlerSettings.Load();
            _requestDelayMs = settings.RequestDelayMs;
            _httpTimeout = TimeSpan.FromSeconds(settings.HttpTimeoutSeconds);
            if (!string.IsNullOrWhiteSpace(projectRoot))
                _metadataService = new MetadataService(projectRoot);
        }

        /// <summary>
        /// Resultado del crawling con información estructurada
        /// </summary>
        public class ResultadoCrawl
        {
            public bool Exitoso { get; set; }
            public string Mensaje { get; set; }
            public int PaginasDescargadas { get; set; }
            public string RutaRelativa { get; set; }
            public Exception Excepcion { get; set; }
        }

        /// <summary>
        /// Realiza crawling de un dominio completo
        /// </summary>
        public ResultadoCrawl CrawlDominio(string urlSemilla, string carpetaGuardado, int maxPaginas = 50, int maxDepth = 2)
        {
            return CrawlDominioAsync(urlSemilla, carpetaGuardado, maxPaginas, maxDepth, CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<ResultadoCrawl> CrawlDominioAsync(
            string urlSemilla,
            string carpetaGuardado,
            int maxPaginas = 50,
            int maxDepth = 2,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var resultado = new ResultadoCrawl();

            try
            {
                Uri startUri;
                if (!Uri.TryCreate(urlSemilla, UriKind.Absolute, out startUri))
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = $"URL inválida: {urlSemilla}";
                    return resultado;
                }

                try
                {
                    Directory.CreateDirectory(carpetaGuardado);
                    if (!Directory.Exists(carpetaGuardado))
                        throw new Exception($"No se pudo crear la carpeta: {carpetaGuardado}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = $"Permiso denegado al crear carpeta: {ex.Message}";
                    resultado.Excepcion = ex;
                    return resultado;
                }
                catch (ArgumentException ex)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = $"Ruta inválida: {ex.Message}";
                    resultado.Excepcion = ex;
                    return resultado;
                }

                var crawlResult = await EjecutarCrawlAsync(
                    startUri,
                    maxPaginas,
                    maxDepth,
                    carpetaGuardado,
                    cancellationToken).ConfigureAwait(false);

                int totalDescargadas = crawlResult.Item1;
                string primerError = crawlResult.Item2;

                resultado.Exitoso = true;
                resultado.PaginasDescargadas = totalDescargadas;
                resultado.RutaRelativa = carpetaGuardado;
                if (totalDescargadas == 0 && primerError != null)
                    resultado.Mensaje = $"0 páginas descargadas. Error: {primerError}";
                else if (totalDescargadas == 0)
                    resultado.Mensaje = "0 páginas (el contenido no superó el filtro de longitud mínima)";
                else
                    resultado.Mensaje = $"Crawling completado: {totalDescargadas} páginas descargadas";
            }
            catch (OperationCanceledException ex)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = "Crawling cancelado por el host.";
                resultado.Excepcion = ex;
            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = $"Error durante el crawling: {ex.Message}";
                resultado.Excepcion = ex;
            }

            return resultado;
        }

        private async Task<Tuple<int, string>> EjecutarCrawlAsync(
            Uri startUri,
            int maxPaginas,
            int maxDepth,
            string carpetaBase,
            CancellationToken cancellationToken)
        {
            var visitadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var enCola = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var cola = new Queue<Tuple<Uri, int>>();
            cola.Enqueue(Tuple.Create(startUri, 0));
            enCola.Add(NormalizarUrl(startUri));

            int contador = 0;
            string primerError = null;
            string jobName = InferirNombreJob(carpetaBase);

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
            };

            using (var client = new HttpClient(handler))
            {
                client.Timeout = _httpTimeout;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("TFG-Crawler/1.0");

                if (EsCrawlerCanariasAzul(startUri))
                    return await EjecutarCrawlCanariasAzulAsync(client, startUri, maxPaginas, carpetaBase, jobName, cancellationToken).ConfigureAwait(false);

                while (cola.Count > 0 && contador < maxPaginas)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var item = cola.Dequeue();
                    var currentUri = item.Item1;
                    int depth = item.Item2;

                    string currentUrl = NormalizarUrl(currentUri);
                    enCola.Remove(currentUrl);
                    if (visitadas.Contains(currentUrl))
                        continue;

                    visitadas.Add(currentUrl);

                    string html;
                    Uri effectiveUri = currentUri;
                    try
                    {
                        using (var response = await client.GetAsync(currentUri).ConfigureAwait(false))
                        {
                            response.EnsureSuccessStatusCode();
                            effectiveUri = response.RequestMessage?.RequestUri ?? currentUri;
                            html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (primerError == null)
                            primerError = $"{currentUri}: {ex.GetBaseException().Message}";

                        await EsperarEntrePeticionesAsync(cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    string effectiveUrl = NormalizarUrl(effectiveUri);
                    if (!string.Equals(effectiveUrl, currentUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        if (visitadas.Contains(effectiveUrl))
                        {
                            await EsperarEntrePeticionesAsync(cancellationToken).ConfigureAwait(false);
                            continue;
                        }

                        visitadas.Add(effectiveUrl);
                    }

                    if (depth < maxDepth)
                    {
                        var enlaces = ExtraerEnlacesInternos(html, effectiveUri, startUri.Host);
                        foreach (var enlace in enlaces)
                        {
                            string enlaceNormalizado = NormalizarUrl(enlace);
                            if (!visitadas.Contains(enlaceNormalizado) && !enCola.Contains(enlaceNormalizado))
                            {
                                cola.Enqueue(Tuple.Create(enlace, depth + 1));
                                enCola.Add(enlaceNormalizado);
                            }
                        }
                    }

                    string titulo = ExtraerTitulo(html);
                    Tuple<string, string> extraccion = ExtraerTextoLimpioConDebug(html, effectiveUri.ToString());
                    string textoLimpio = extraccion.Item1;
                    string textoPreFiltros = extraccion.Item2;
                    Quality calidad = QualityScorer.Score(textoLimpio);

                    if (calidad != Quality.Ok)
                    {
                        if (!EsPaginaDeBajoValor(effectiveUri.ToString()))
                            GuardarDebugExtraccion(carpetaBase, effectiveUri, html, textoPreFiltros, textoLimpio, calidad);
                        await EsperarEntrePeticionesAsync(cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    string nombreArchivo = GenerarNombreSeguro(effectiveUri, contador + 1);
                    PersistirDocumento(carpetaBase, nombreArchivo, textoLimpio, effectiveUri.ToString(), titulo, jobName, depth, ref contador);

                    await EsperarEntrePeticionesAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            return Tuple.Create(contador, primerError);
        }

        private async Task<Tuple<int, string>> EjecutarCrawlCanariasAzulAsync(
            HttpClient client,
            Uri startUri,
            int maxPaginas,
            string carpetaBase,
            string jobName,
            CancellationToken cancellationToken)
        {
            int contador = 0;
            string primerError = null;
            var slugsProcesados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var paginasVistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Uri catalogoUri = new Uri(startUri, "Catalogo");

            try
            {
                string htmlHome = await DescargarHtmlAsync(client, startUri, cancellationToken).ConfigureAwait(false);
                string tituloHome = ExtraerTitulo(htmlHome);
                Tuple<string, string> extraccionHome = ExtraerTextoLimpioConDebug(htmlHome, startUri.ToString());
                string nombreHome = GenerarNombreSeguro(startUri, contador + 1);
                if (!TryPersistirDocumentoIndexable(carpetaBase, startUri, nombreHome, extraccionHome, tituloHome, jobName, 0, ref contador))
                    GuardarDebugExtraccion(carpetaBase, startUri, htmlHome, extraccionHome.Item2, extraccionHome.Item1, QualityScorer.Score(extraccionHome.Item1));
            }
            catch (Exception ex)
            {
                primerError = $"{startUri}: {ex.GetBaseException().Message}";
            }

            if (contador >= maxPaginas)
                return Tuple.Create(contador, primerError);

            await EsperarEntrePeticionesAsync(cancellationToken).ConfigureAwait(false);

            WebFormsPageState estadoListado;
            try
            {
                string htmlCatalogo = await DescargarHtmlAsync(client, catalogoUri, cancellationToken).ConfigureAwait(false);
                estadoListado = CrearEstadoWebForms(catalogoUri, htmlCatalogo);
            }
            catch (Exception ex)
            {
                if (primerError == null)
                    primerError = $"{catalogoUri}: {ex.GetBaseException().Message}";
                return Tuple.Create(contador, primerError);
            }

            while (estadoListado != null && contador < maxPaginas)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string firmaPagina = CrearFirmaPaginaCatalogo(estadoListado.Html);
                if (!paginasVistas.Add(firmaPagina))
                    break;

                foreach (CatalogoListadoItem item in ExtraerItemsCatalogo(estadoListado.Html))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (contador >= maxPaginas)
                        break;

                    string slug = CrearSlug(item.Title);
                    if (string.IsNullOrWhiteSpace(slug) || !slugsProcesados.Add(slug))
                        continue;

                    WebFormsPageState estadoDetalle;
                    try
                    {
                        estadoDetalle = await HacerPostbackWebFormsAsync(
                            client,
                            estadoListado,
                            item.EventTarget,
                            string.Empty,
                            cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (primerError == null)
                            primerError = $"{catalogoUri}#{slug}: {ex.GetBaseException().Message}";
                        continue;
                    }

                    string tituloDetalle;
                    string textoDetalle = ExtraerTextoDetalleCanariasAzul(estadoDetalle.Html, out tituloDetalle);
                    tituloDetalle = string.IsNullOrWhiteSpace(tituloDetalle) ? item.Title : tituloDetalle;
                    string urlDetalle = ConstruirUrlDetalleCanariasAzul(catalogoUri, slug);
                    string nombreArchivo = GenerarNombreCatalogoItem(contador + 1, slug);
                    var extraccionDetalle = Tuple.Create(textoDetalle, textoDetalle);

                    if (!TryPersistirDocumentoIndexable(carpetaBase, new Uri(urlDetalle), nombreArchivo, extraccionDetalle, tituloDetalle, jobName, 1, ref contador, urlDetalle))
                        GuardarDebugExtraccion(carpetaBase, new Uri(catalogoUri, "item-" + slug), estadoDetalle.Html, textoDetalle, textoDetalle, QualityScorer.Score(textoDetalle));

                    estadoListado = await RestaurarEstadoListadoCanariasAzulAsync(client, estadoDetalle, cancellationToken).ConfigureAwait(false);
                    await EsperarEntrePeticionesAsync(cancellationToken).ConfigureAwait(false);
                }

                if (contador >= maxPaginas)
                    break;

                string siguienteEventTarget = ExtraerEventTargetSiguientePagina(estadoListado.Html);
                if (string.IsNullOrWhiteSpace(siguienteEventTarget))
                    break;

                try
                {
                    estadoListado = await HacerPostbackWebFormsAsync(
                        client,
                        estadoListado,
                        siguienteEventTarget,
                        string.Empty,
                        cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (primerError == null)
                        primerError = $"{catalogoUri}: {ex.GetBaseException().Message}";
                    break;
                }

                await EsperarEntrePeticionesAsync(cancellationToken).ConfigureAwait(false);
            }

            return Tuple.Create(contador, primerError);
        }

        private Task EsperarEntrePeticionesAsync(CancellationToken cancellationToken)
        {
            if (_requestDelayMs <= 0)
                return Task.CompletedTask;

            return Task.Delay(_requestDelayMs, cancellationToken);
        }

        private List<Uri> ExtraerEnlacesInternos(string html, Uri baseUri, string hostObjetivo)
        {
            var resultado = new List<Uri>();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var links = doc.DocumentNode.SelectNodes("//a[@href]");
            if (links == null)
                return resultado;

            foreach (var link in links)
            {
                var href = link.GetAttributeValue("href", string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(href))
                    continue;

                if (href.StartsWith("#") ||
                    href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                    href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                    href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Uri nuevaUri;
                if (Uri.TryCreate(baseUri, href, out nuevaUri))
                {
                    if (!EsUrlRastreable(nuevaUri))
                        continue;

                    if (string.Equals(nuevaUri.Host, hostObjetivo, StringComparison.OrdinalIgnoreCase))
                        resultado.Add(nuevaUri);
                }
            }

            return resultado;
        }

        private bool EsUrlRastreable(Uri uri)
        {
            if (!(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                return false;

            string path = uri.AbsolutePath.ToLowerInvariant();
            string[] extensionesNoDeseadas =
            {
                ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg",
                ".pdf", ".zip", ".rar", ".7z",
                ".mp4", ".mp3", ".wav",
                ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"
            };

            return !extensionesNoDeseadas.Any(ext => path.EndsWith(ext));
        }

        public string ExtraerTextoLimpio(string html, string debugUrl = null)
        {
            return ExtraerTextoLimpioConDebug(html, debugUrl).Item1;
        }

        private Tuple<string, string> ExtraerTextoLimpioConDebug(string html, string debugUrl = null)
        {
            if (EsPaginaDeBajoValor(debugUrl))
                return Tuple.Create(string.Empty, string.Empty);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            EliminarNodos(doc, _nodosBasura);
            EliminarNodos(doc, _nodosInteractivos);
            EliminarNodosOcultosODecorativos(doc);
            EliminarNodosPorAtributosDeRuido(doc);

            HtmlNode contenido = SeleccionarContenidoPrincipal(doc);
            var bloques = ExtraerBloquesSemanticos(contenido, false);

            string textoAntesFiltros = string.Join(Environment.NewLine + Environment.NewLine, bloques);
            if (ExtraccionInsuficiente(bloques))
            {
                var docLigero = new HtmlDocument();
                docLigero.LoadHtml(html);

                EliminarNodos(docLigero, _nodosBasuraFallback);

                HtmlNode mejorContenedor = SeleccionarContenidoPrincipal(docLigero);
                if (mejorContenedor != null)
                {
                    var bloquesRespaldo = ExtraerBloquesSemanticos(mejorContenedor, false);
                    if (TieneMasContenido(bloquesRespaldo, bloques))
                        bloques = bloquesRespaldo;

                    if (ExtraccionInsuficiente(bloques))
                    {
                        string textoRespaldo = NormalizarTexto(HtmlEntity.DeEntitize(mejorContenedor.InnerText));
                        if (!string.IsNullOrWhiteSpace(textoRespaldo))
                            bloques = new List<string> { textoRespaldo };
                    }
                }
            }

            textoAntesFiltros = string.Join(Environment.NewLine + Environment.NewLine, bloques);
            bloques = DepurarBloquesParaRag(bloques);
            string textoFinal = string.Join(Environment.NewLine + Environment.NewLine, bloques);
            return Tuple.Create(textoFinal, textoAntesFiltros);
        }

        private bool TryPersistirDocumentoIndexable(
            string carpetaBase,
            Uri debugUri,
            string nombreArchivo,
            Tuple<string, string> extraccion,
            string titulo,
            string jobName,
            int depth,
            ref int contador,
            string urlMetadata = null)
        {
            string textoLimpio = extraccion?.Item1 ?? string.Empty;
            Quality calidad = QualityScorer.Score(textoLimpio);
            if (calidad != Quality.Ok)
                return false;

            PersistirDocumento(
                carpetaBase,
                nombreArchivo,
                textoLimpio,
                urlMetadata ?? debugUri.ToString(),
                titulo,
                jobName,
                depth,
                ref contador);

            return true;
        }

        private void PersistirDocumento(
            string carpetaBase,
            string nombreArchivo,
            string textoLimpio,
            string url,
            string titulo,
            string jobName,
            int depth,
            ref int contador)
        {
            string rutaArchivo = Path.Combine(carpetaBase, nombreArchivo);
            File.WriteAllText(rutaArchivo, (textoLimpio ?? string.Empty).TrimStart('\uFEFF'), new UTF8Encoding(false));
            contador++;

            if (_metadataService != null)
            {
                try
                {
                    var meta = _metadataService.BuildForNewPage(
                        rutaArchivo,
                        url,
                        titulo,
                        jobName,
                        contador,
                        DateTime.UtcNow,
                        depth);
                    _metadataService.UpsertAndSave(meta);
                }
                catch
                {
                }
            }
        }

        private async Task<string> DescargarHtmlAsync(HttpClient client, Uri uri, CancellationToken cancellationToken)
        {
            using (var response = await client.GetAsync(uri, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        private async Task<WebFormsPageState> HacerPostbackWebFormsAsync(
            HttpClient client,
            WebFormsPageState state,
            string eventTarget,
            string eventArgument,
            CancellationToken cancellationToken)
        {
            var payload = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in state.HiddenFields)
                payload[item.Key] = item.Value ?? string.Empty;

            payload["__EVENTTARGET"] = eventTarget ?? string.Empty;
            payload["__EVENTARGUMENT"] = eventArgument ?? string.Empty;
            payload["__LASTFOCUS"] = string.Empty;

            using (var request = new HttpRequestMessage(HttpMethod.Post, state.FormAction))
            {
                request.Headers.Referrer = state.FormAction;
                request.Content = new FormUrlEncodedContent(payload);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                using (var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    string html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Uri effectiveUri = response.RequestMessage?.RequestUri ?? state.FormAction;
                    return CrearEstadoWebForms(effectiveUri, html);
                }
            }
        }

        private async Task<WebFormsPageState> RestaurarEstadoListadoCanariasAzulAsync(
            HttpClient client,
            WebFormsPageState estadoActual,
            CancellationToken cancellationToken)
        {
            if (estadoActual == null || !TieneEventTarget(estadoActual.Html, EventTargetCerrarDetalleCanariasAzul))
                return estadoActual;

            try
            {
                return await HacerPostbackWebFormsAsync(
                    client,
                    estadoActual,
                    EventTargetCerrarDetalleCanariasAzul,
                    string.Empty,
                    cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return estadoActual;
            }
        }

        private static WebFormsPageState CrearEstadoWebForms(Uri pageUri, string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html ?? string.Empty);

            HtmlNode formNode = doc.DocumentNode.SelectSingleNode("//form[@method='post']") ??
                                doc.DocumentNode.SelectSingleNode("//form");

            string action = formNode?.GetAttributeValue("action", null);
            Uri formAction = string.IsNullOrWhiteSpace(action) ? pageUri : new Uri(pageUri, action);
            var hiddenFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            HtmlNodeCollection inputs = doc.DocumentNode.SelectNodes("//form//input[@name]");
            if (inputs != null)
            {
                foreach (var input in inputs)
                {
                    string type = (input.GetAttributeValue("type", string.Empty) ?? string.Empty).ToLowerInvariant();
                    if (type != "hidden")
                        continue;

                    string name = input.GetAttributeValue("name", string.Empty);
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    hiddenFields[name] = input.GetAttributeValue("value", string.Empty) ?? string.Empty;
                }
            }

            return new WebFormsPageState
            {
                Html = html ?? string.Empty,
                PageUri = pageUri,
                FormAction = formAction,
                HiddenFields = hiddenFields
            };
        }

        private List<CatalogoListadoItem> ExtraerItemsCatalogo(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html ?? string.Empty);
            var resultado = new List<CatalogoListadoItem>();

            HtmlNodeCollection links = doc.DocumentNode.SelectNodes("//a[contains(@id,'ListadoElementos') and contains(@id,'BotonDetallesItem')]");
            if (links == null)
                return resultado;

            foreach (var link in links)
            {
                string eventTarget = DecodificarEventTargetDesdeHref(link.GetAttributeValue("href", string.Empty));
                string titulo = NormalizarTexto(HtmlEntity.DeEntitize(link.SelectSingleNode(".//h4")?.InnerText ?? string.Empty));
                if (string.IsNullOrWhiteSpace(eventTarget) || string.IsNullOrWhiteSpace(titulo))
                    continue;

                resultado.Add(new CatalogoListadoItem
                {
                    Title = titulo,
                    EventTarget = eventTarget
                });
            }

            return resultado;
        }

        private string ExtraerEventTargetSiguientePagina(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html ?? string.Empty);
            HtmlNode nextLink = doc.DocumentNode.SelectSingleNode("//span[contains(@id,'PaginadorResultados')]//a[contains(@class,'pagina-siguiente')]");
            return nextLink == null ? null : DecodificarEventTargetDesdeHref(nextLink.GetAttributeValue("href", string.Empty));
        }

        private static string DecodificarEventTargetDesdeHref(string href)
        {
            if (string.IsNullOrWhiteSpace(href))
                return null;

            string decoded = HtmlEntity.DeEntitize(href);
            var match = Regex.Match(decoded, @"__doPostBack\('(?<target>[^']+)'(?:,'[^']*')?\)");
            return match.Success ? match.Groups["target"].Value : null;
        }

        private string ExtraerTextoDetalleCanariasAzul(string html, out string titulo)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html ?? string.Empty);
            HtmlNode panel = doc.DocumentNode.SelectSingleNode("//div[@id='ctl00_MainContent_DatosElementoPatrimonial']") ??
                             doc.DocumentNode.SelectSingleNode("//div[contains(@id,'DatosElementoPatrimonial')]");

            titulo = string.Empty;
            if (panel == null)
                return string.Empty;

            titulo = NormalizarTexto(HtmlEntity.DeEntitize(
                panel.SelectSingleNode(".//div[contains(@class,'section-title')]//h2")?.InnerText ?? string.Empty));

            var bloques = new List<string>();
            if (!string.IsNullOrWhiteSpace(titulo))
                bloques.Add(titulo);

            string categoria = ExtraerCampoDetalleCanariasAzul(panel.SelectSingleNode(".//div[contains(@class,'categorias-item')]"));
            if (!string.IsNullOrWhiteSpace(categoria))
                bloques.Add(categoria);

            string descripcion = ExtraerTextoNodoComoParrafos(panel.SelectSingleNode(".//div[contains(@class,'descripcion-item')]"));
            if (!string.IsNullOrWhiteSpace(descripcion))
                bloques.Add(descripcion);

            HtmlNodeCollection campos = panel.SelectNodes(".//section[contains(@class,'datos-adicionales-item')]//li/div");
            if (campos != null)
            {
                foreach (var campo in campos)
                {
                    string valor = ExtraerCampoDetalleCanariasAzul(campo);
                    if (!string.IsNullOrWhiteSpace(valor))
                        bloques.Add(valor);
                }
            }

            string pieImagen = ExtraerTextoNodoComoParrafos(panel.SelectSingleNode(".//div[contains(@class,'pie_imagen_principal')]"));
            if (!string.IsNullOrWhiteSpace(pieImagen))
                bloques.Add(pieImagen);

            return string.Join(Environment.NewLine + Environment.NewLine, bloques
                .Select(NormalizarBloqueMultilinea)
                .Where(EsBloqueIndexable)
                .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private string ExtraerCampoDetalleCanariasAzul(HtmlNode nodo)
        {
            if (nodo == null)
                return string.Empty;

            string etiqueta = NormalizarTexto(HtmlEntity.DeEntitize(
                nodo.SelectSingleNode(".//*[contains(@class,'cabecera')]//span")?.InnerText ??
                nodo.SelectSingleNode(".//*[contains(@class,'cabecera')]")?.InnerText ??
                string.Empty));

            var clone = HtmlNode.CreateNode(nodo.OuterHtml);
            foreach (var basura in clone.SelectNodes(".//i|.//*[contains(@class,'cabecera')]") ?? Enumerable.Empty<HtmlNode>())
                basura.Remove();

            string texto = ExtraerTextoNodoComoParrafos(clone);
            string enlace = clone.SelectSingleNode(".//a[@href]")?.GetAttributeValue("href", string.Empty) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(enlace) && !string.Equals(enlace, "#", StringComparison.OrdinalIgnoreCase))
                texto = string.IsNullOrWhiteSpace(texto) ? enlace : $"{texto} {enlace}".Trim();

            texto = NormalizarTexto(texto);
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(etiqueta) && texto.StartsWith(etiqueta, StringComparison.OrdinalIgnoreCase))
                texto = texto.Substring(etiqueta.Length).TrimStart(':', ' ');

            return string.IsNullOrWhiteSpace(etiqueta) ? texto : $"{etiqueta}: {texto}";
        }

        private string ExtraerTextoNodoComoParrafos(HtmlNode nodo)
        {
            if (nodo == null)
                return string.Empty;

            HtmlNodeCollection textNodes = nodo.SelectNodes(".//p|.//li|.//h3|.//h4|.//a");
            if (textNodes == null || textNodes.Count == 0)
                return NormalizarTexto(HtmlEntity.DeEntitize(nodo.InnerText));

            var piezas = new List<string>();
            foreach (var child in textNodes)
            {
                string texto = NormalizarTexto(HtmlEntity.DeEntitize(child.InnerText));
                if (!string.IsNullOrWhiteSpace(texto))
                    piezas.Add(texto);
            }

            return string.Join(Environment.NewLine, piezas.Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static bool TieneEventTarget(string html, string eventTarget)
        {
            return !string.IsNullOrWhiteSpace(html) &&
                   !string.IsNullOrWhiteSpace(eventTarget) &&
                   html.IndexOf(eventTarget, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string CrearFirmaPaginaCatalogo(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            string paginador = HtmlEntity.DeEntitize(doc.DocumentNode.SelectSingleNode("//span[contains(@id,'PaginadorResultados')]")?.InnerText ?? string.Empty);
            string primerTitulo = HtmlEntity.DeEntitize(doc.DocumentNode.SelectSingleNode("//a[contains(@id,'ListadoElementos') and contains(@id,'BotonDetallesItem')]//h4")?.InnerText ?? string.Empty);
            return NormalizarTexto(paginador + "|" + primerTitulo);
        }

        private static string ConstruirUrlDetalleCanariasAzul(Uri catalogoUri, string slug)
        {
            return catalogoUri.GetLeftPart(UriPartial.Path).TrimEnd('/') + "#item-" + slug;
        }

        private string GenerarNombreCatalogoItem(int numero, string slug)
        {
            string baseSlug = string.IsNullOrWhiteSpace(slug) ? "item" : slug;
            if (baseSlug.Length > 60)
                baseSlug = baseSlug.Substring(0, 60);

            return $"{numero:D2}_item_{baseSlug}.txt";
        }

        private static bool EsCrawlerCanariasAzul(Uri uri)
        {
            return uri != null &&
                   string.Equals(uri.Host, HostCanariasAzul, StringComparison.OrdinalIgnoreCase);
        }

        private static string CrearSlug(string text)
        {
            string normalized = (text ?? string.Empty).ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (char c in normalized)
            {
                var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == System.Globalization.UnicodeCategory.NonSpacingMark)
                    continue;

                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                    sb.Append(c);
                else if (sb.Length > 0 && sb[sb.Length - 1] != '-')
                    sb.Append('-');
            }

            string slug = sb.ToString().Trim('-');
            return string.IsNullOrWhiteSpace(slug) ? "item" : slug;
        }

        private void EliminarNodos(HtmlDocument doc, IEnumerable<string> nodos)
        {
            var xpath = string.Join("|", nodos.Select(t => "//" + t));
            var encontrados = doc.DocumentNode.SelectNodes(xpath);
            if (encontrados == null)
                return;

            foreach (var nodo in encontrados.ToList())
                nodo.Remove();
        }

        private List<string> ExtraerBloquesSemanticos(HtmlNode contenedor, bool aplicarFiltros = true)
        {
            if (contenedor == null)
                return new List<string>();

            var bloques = new List<BloqueContenido>();
            var nodosUtiles = contenedor.SelectNodes(
                ".//h1|.//h2|.//h3|.//h4|.//h5|.//h6|.//p|.//li|.//blockquote|.//figcaption|.//td|.//th|.//dt|.//dd");

            if (nodosUtiles == null || nodosUtiles.Count == 0)
            {
                string textoPlano = NormalizarTexto(HtmlEntity.DeEntitize(contenedor.InnerText));
                if (!aplicarFiltros)
                    return string.IsNullOrWhiteSpace(textoPlano) ? new List<string>() : new List<string> { textoPlano };

                if (EsBloqueIndexable(textoPlano))
                    return new List<string> { textoPlano };

                return new List<string>();
            }

            BloqueContenido bloqueActual = null;

            foreach (var nodo in nodosUtiles)
            {
                string texto = NormalizarTexto(HtmlEntity.DeEntitize(nodo.InnerText));
                if (string.IsNullOrWhiteSpace(texto))
                    continue;

                if (EsEncabezado(nodo.Name))
                {
                    if (bloqueActual != null)
                        bloques.Add(bloqueActual);

                    bloqueActual = new BloqueContenido
                    {
                        Titulo = texto,
                        NivelTitulo = ObtenerNivelTitulo(nodo.Name)
                    };
                    continue;
                }

                if (bloqueActual == null)
                    bloqueActual = new BloqueContenido();

                bloqueActual.Fragmentos.Add(texto);
            }

            if (bloqueActual != null)
                bloques.Add(bloqueActual);

            var renderizados = bloques
                .Select(RenderizarBloque)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!aplicarFiltros)
                return renderizados;

            return renderizados
                .Where(EsBloqueIndexable)
                .ToList();
        }

        private bool ExtraccionInsuficiente(List<string> bloques)
        {
            string texto = string.Join(Environment.NewLine + Environment.NewLine, bloques ?? new List<string>());
            return bloques == null || bloques.Count == 0 || texto.Length < 300;
        }

        private bool TieneMasContenido(List<string> candidato, List<string> actual)
        {
            int longitudActual = string.Join(Environment.NewLine + Environment.NewLine, actual ?? new List<string>()).Length;
            int longitudCandidata = string.Join(Environment.NewLine + Environment.NewLine, candidato ?? new List<string>()).Length;
            return longitudCandidata > longitudActual;
        }

        private List<string> DepurarBloquesParaRag(List<string> bloques)
        {
            var resultado = new List<string>();

            foreach (string original in bloques)
            {
                string bloque = original ?? string.Empty;
                if (string.IsNullOrWhiteSpace(bloque))
                    continue;

                bloque = NormalizarBloqueMultilinea(bloque);
                if (EsTeaserTruncado(bloque))
                    continue;

                if (_patronesBoilerplate.Any(p => bloque.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;

                if (resultado.Any(r => SonBloquesEquivalentes(r, bloque)))
                    continue;

                int indiceDuplicado = resultado.FindIndex(r => ContieneBloqueConEtiquetaDuplicada(r, bloque));
                if (indiceDuplicado >= 0)
                {
                    resultado[indiceDuplicado] = ElegirMejorBloque(resultado[indiceDuplicado], bloque);
                    continue;
                }

                resultado.Add(bloque);
            }

            return resultado
                .Where(EsBloqueIndexable)
                .ToList();
        }

        private bool EsBloqueIndexable(string bloque)
        {
            if (string.IsNullOrWhiteSpace(bloque))
                return false;

            string normalizado = NormalizarTextoParaIndice(bloque);
            if (normalizado.Length < 40)
                return false;

            if (EsTeaserTruncado(normalizado))
                return false;

            int coincidenciasRuido = _patronesRuido.Count(p =>
                normalizado.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);

            int coincidenciasBoilerplate = _patronesBoilerplate.Count(p =>
                normalizado.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);

            return coincidenciasRuido < 2 && coincidenciasBoilerplate == 0;
        }

        private string RenderizarBloque(BloqueContenido bloque)
        {
            string titulo = NormalizarTexto(bloque.Titulo);
            var descripcion = new List<string>();
            var metadata = new List<string>();

            foreach (string fragmentoOriginal in bloque.Fragmentos.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string fragmento = NormalizarTexto(fragmentoOriginal);
                if (string.IsNullOrWhiteSpace(fragmento))
                    continue;

                fragmento = RepararPuntuacionPegada(fragmento);

                string claveMetadata;
                if (EsLineaMetadata(fragmento, out claveMetadata))
                {
                    if (DebeDescartarMetadata(claveMetadata, fragmento))
                        continue;

                    if (EsDescripcionMetadata(claveMetadata))
                    {
                        string textoDescripcion = ExtraerValorMetadata(fragmento);
                        if (!string.IsNullOrWhiteSpace(textoDescripcion))
                            descripcion.Add(textoDescripcion);
                        continue;
                    }

                    metadata.Add(FormatearMetadata(fragmento));
                    continue;
                }

                descripcion.Add(fragmento);
            }

            descripcion = descripcion
                .Select(NormalizarTexto)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            metadata = metadata
                .Select(NormalizarTexto)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(titulo))
                partes.Add(titulo);

            if (descripcion.Count > 0)
                partes.Add(string.Join(Environment.NewLine, descripcion));

            if (metadata.Count > 0)
                partes.Add(string.Join(Environment.NewLine, metadata));

            return string.Join(Environment.NewLine + Environment.NewLine, partes);
        }

        private bool EsEncabezado(string nodeName)
        {
            return nodeName.Length == 2 &&
                   nodeName[0] == 'h' &&
                   char.IsDigit(nodeName[1]);
        }

        private int ObtenerNivelTitulo(string nodeName)
        {
            return EsEncabezado(nodeName) ? nodeName[1] - '0' : 0;
        }

        private string NormalizarTexto(string texto)
        {
            texto = texto ?? string.Empty;
            texto = Regex.Replace(texto, @"\s+", " ").Trim();
            return texto;
        }

        private string NormalizarBloqueMultilinea(string texto)
        {
            var lineas = (texto ?? string.Empty)
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(l => RepararPuntuacionPegada(NormalizarTexto(l)))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            return string.Join(Environment.NewLine, lineas);
        }

        private string NormalizarTextoParaIndice(string texto)
        {
            return NormalizarTexto((texto ?? string.Empty)
                .Replace("\r", " ")
                .Replace("\n", " "));
        }

        private string RepararPuntuacionPegada(string texto)
        {
            texto = Regex.Replace(texto, @"([a-záéíóúñ])\.([A-ZÁÉÍÓÚÑ])", "$1. $2");
            texto = Regex.Replace(texto, @"([a-záéíóúñ])\:([A-ZÁÉÍÓÚÑ])", "$1: $2");
            return texto;
        }

        private bool EsTeaserTruncado(string texto)
        {
            texto = NormalizarTextoParaIndice(texto);
            if (string.IsNullOrWhiteSpace(texto))
                return false;

            return texto.EndsWith("...", StringComparison.Ordinal) ||
                   texto.EndsWith("…", StringComparison.Ordinal) ||
                   Regex.IsMatch(texto, @"\b\w+\.\.\.$");
        }

        private bool SonBloquesEquivalentes(string a, string b)
        {
            string na = NormalizarComparacion(a);
            string nb = NormalizarComparacion(b);
            return string.Equals(na, nb, StringComparison.OrdinalIgnoreCase);
        }

        private bool ContieneBloqueConEtiquetaDuplicada(string existente, string candidato)
        {
            string existenteSinEtiqueta = QuitarEtiquetaInicial(existente);
            string candidatoSinEtiqueta = QuitarEtiquetaInicial(candidato);

            if (string.IsNullOrWhiteSpace(existenteSinEtiqueta) || string.IsNullOrWhiteSpace(candidatoSinEtiqueta))
                return false;

            string ne = NormalizarComparacion(existenteSinEtiqueta);
            string nc = NormalizarComparacion(candidatoSinEtiqueta);

            return ne.Contains(nc) || nc.Contains(ne);
        }

        private string ElegirMejorBloque(string actual, string candidato)
        {
            bool actualTieneEtiqueta = TieneEtiquetaInicial(actual);
            bool candidatoTieneEtiqueta = TieneEtiquetaInicial(candidato);

            if (actualTieneEtiqueta && !candidatoTieneEtiqueta)
                return actual;

            if (!actualTieneEtiqueta && candidatoTieneEtiqueta)
                return candidato;

            return actual.Length >= candidato.Length ? actual : candidato;
        }

        private bool TieneEtiquetaInicial(string texto)
        {
            return Regex.IsMatch(texto ?? string.Empty, @"^[A-ZÁÉÍÓÚÑ][^:\r\n]{1,40}\:\s+");
        }

        private string QuitarEtiquetaInicial(string texto)
        {
            return Regex.Replace(texto ?? string.Empty, @"^[A-ZÁÉÍÓÚÑ][^:\r\n]{1,40}\:\s+", string.Empty).Trim();
        }

        private string NormalizarComparacion(string texto)
        {
            texto = QuitarEtiquetaInicial(texto);
            texto = NormalizarTextoParaIndice(texto).ToLowerInvariant();
            return texto;
        }

        private bool EsLineaMetadata(string texto, out string clave)
        {
            var match = Regex.Match(texto ?? string.Empty, @"^(?<clave>[A-ZÁÉÍÓÚÑa-záéíóúñ][^:\r\n]{1,40})\:\s+(?<valor>.+)$");
            if (!match.Success)
            {
                clave = null;
                return false;
            }

            clave = NormalizarTexto(match.Groups["clave"].Value).ToLowerInvariant();
            return true;
        }

        private string ExtraerValorMetadata(string texto)
        {
            var match = Regex.Match(texto ?? string.Empty, @"^[^:\r\n]{1,40}\:\s+(?<valor>.+)$");
            return match.Success ? NormalizarTexto(match.Groups["valor"].Value) : string.Empty;
        }

        private bool EsDescripcionMetadata(string clave)
        {
            return string.Equals(clave, "descripción", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(clave, "descripcion", StringComparison.OrdinalIgnoreCase);
        }

        private bool DebeDescartarMetadata(string clave, string linea)
        {
            if (_camposMetadataDescartables.Any(c => string.Equals(c, clave, StringComparison.OrdinalIgnoreCase)))
                return true;

            string valor = ExtraerValorMetadata(linea);
            if (string.IsNullOrWhiteSpace(valor))
                return true;

            string valorNormalizado = valor.ToLowerInvariant();
            if (valorNormalizado == "desconocido" || valorNormalizado == "undefined")
                return false;

            if (!_camposMetadataPrioritarios.Any(c => string.Equals(c, clave, StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }

        private string FormatearMetadata(string linea)
        {
            var match = Regex.Match(linea ?? string.Empty, @"^(?<clave>[^:\r\n]{1,40})\:\s+(?<valor>.+)$");
            if (!match.Success)
                return NormalizarTexto(linea);

            string clave = NormalizarTexto(match.Groups["clave"].Value);
            string valor = NormalizarTexto(match.Groups["valor"].Value);
            return $"{clave}: {valor}";
        }

        private bool EsPaginaDeBajoValor(string debugUrl)
        {
            if (string.IsNullOrWhiteSpace(debugUrl))
                return false;

            Uri uri;
            if (!Uri.TryCreate(debugUrl, UriKind.Absolute, out uri))
                return false;

            string url = uri.AbsoluteUri.ToLowerInvariant();
            return _tokensRutasBajoValor.Any(token => url.Contains(token));
        }

        private HtmlNode SeleccionarContenidoPrincipal(HtmlDocument doc)
        {
            string[] xpathsDirectos =
            {
                "//main",
                "//*[@role='main']",
                "//article",
                "//section",
                "//div[contains(@class,'entry-content')]",
                "//div[contains(@class,'page-content')]",
                "//div[contains(@class,'post-content')]",
                "//div[contains(@class,'post-body')]",
                "//div[contains(@class,'post__content')]",
                "//div[contains(@class,'site-main')]",
                "//div[contains(@class,'elementor-widget-theme-post-content')]",
                "//div[contains(@class,'elementor-location-single')]",
                "//div[contains(@class,'elementor-widget-container')]",
                "//*[@id='content']",
                "//*[@id='main']",
                "//div[@id='content']",
                "//div[@id='main']",
                "//div[contains(@class,'content')]"
            };

            var candidatosDirectos = xpathsDirectos
                .SelectMany(xpath => doc.DocumentNode.SelectNodes(xpath) ?? Enumerable.Empty<HtmlNode>())
                .Where(n => n != null)
                .Distinct(new HtmlNodeReferenceComparer())
                .ToList();

            if (candidatosDirectos.Any())
                return candidatosDirectos
                    .Where(TieneTextoVisibleSuficiente)
                    .OrderByDescending(CalcularPuntuacionContenido)
                    .FirstOrDefault() ?? candidatosDirectos.OrderByDescending(CalcularPuntuacionContenido).First();

            var candidatos = doc.DocumentNode.SelectNodes("//body|//form|//section|//article|//main|//div");
            if (candidatos == null || candidatos.Count == 0)
                return doc.DocumentNode.SelectSingleNode("//body");

            var mejor = candidatos
                .Where(TieneTextoVisibleSuficiente)
                .OrderByDescending(CalcularPuntuacionContenido)
                .FirstOrDefault();

            return mejor ?? doc.DocumentNode.SelectSingleNode("//body");
        }

        private sealed class HtmlNodeReferenceComparer : IEqualityComparer<HtmlNode>
        {
            public bool Equals(HtmlNode x, HtmlNode y)
            {
                return object.ReferenceEquals(x, y);
            }

            public int GetHashCode(HtmlNode obj)
            {
                return obj == null ? 0 : obj.GetHashCode();
            }
        }

        private bool TieneTextoVisibleSuficiente(HtmlNode nodo)
        {
            string texto = HtmlEntity.DeEntitize(nodo.InnerText ?? string.Empty);
            texto = Regex.Replace(texto, @"\s+", " ").Trim();
            return texto.Length >= 120;
        }

        private int CalcularPuntuacionContenido(HtmlNode nodo)
        {
            var nodosUtiles = nodo.SelectNodes(".//h1|.//h2|.//h3|.//h4|.//h5|.//h6|.//p|.//li|.//blockquote|.//figcaption|.//td|.//th|.//dt|.//dd");
            int cuentaUtiles = nodosUtiles?.Count ?? 0;

            string texto = HtmlEntity.DeEntitize(nodo.InnerText ?? string.Empty);
            texto = Regex.Replace(texto, @"\s+", " ").Trim();

            int penalizacionRuido = _tokensRuidoAtributos.Count(token =>
                ContieneTokenAtributo(nodo, token)) * 50;

            return (cuentaUtiles * 40) + Math.Min(texto.Length, 4000) - penalizacionRuido;
        }

        private void EliminarNodosOcultosODecorativos(HtmlDocument doc)
        {
            var nodos = doc.DocumentNode.SelectNodes(
                "//*[@hidden or @aria-hidden='true' or contains(translate(@style,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'display:none') or contains(translate(@style,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'visibility:hidden')]");

            if (nodos == null)
                return;

            foreach (var nodo in nodos.ToList())
                nodo.Remove();
        }

        private void EliminarNodosPorAtributosDeRuido(HtmlDocument doc)
        {
            var candidatos = doc.DocumentNode.SelectNodes("//*[@id or @class or @role or @aria-label]");
            if (candidatos == null)
                return;

            foreach (var nodo in candidatos.ToList())
            {
                if (EsNodoDeRuido(nodo) && DebeEliminarNodoDeRuido(nodo))
                    nodo.Remove();
            }
        }

        private bool EsNodoDeRuido(HtmlNode nodo)
        {
            string role = (nodo.GetAttributeValue("role", string.Empty) ?? string.Empty).ToLowerInvariant();
            if (role == "navigation" || role == "search" || role == "complementary" ||
                role == "contentinfo" || role == "dialog" || role == "banner")
            {
                return true;
            }

            return _tokensRuidoAtributos.Any(token => ContieneTokenAtributo(nodo, token));
        }

        private bool ContieneTokenAtributo(HtmlNode nodo, string token)
        {
            string id = nodo.GetAttributeValue("id", string.Empty) ?? string.Empty;
            string cssClass = nodo.GetAttributeValue("class", string.Empty) ?? string.Empty;
            string ariaLabel = nodo.GetAttributeValue("aria-label", string.Empty) ?? string.Empty;

            return id.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   cssClass.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   ariaLabel.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool DebeEliminarNodoDeRuido(HtmlNode nodo)
        {
            if (nodo == null)
                return false;

            if (nodo.Name == "main" || nodo.Name == "article" || nodo.Name == "section")
                return false;

            var nodosUtiles = nodo.SelectNodes(".//h1|.//h2|.//h3|.//h4|.//h5|.//h6|.//p|.//li|.//blockquote|.//figcaption|.//td|.//th|.//dt|.//dd");
            if ((nodosUtiles?.Count ?? 0) >= 3)
                return false;

            string texto = NormalizarTexto(HtmlEntity.DeEntitize(nodo.InnerText));
            return texto.Length < 400;
        }

        private string GenerarNombreSeguro(Uri uri, int numero)
        {
            string path = uri.AbsolutePath.Trim('/');

            if (string.IsNullOrWhiteSpace(path))
                path = "home";

            path = path.Replace("/", "_");

            foreach (char c in Path.GetInvalidFileNameChars())
                path = path.Replace(c, '_');

            if (path.Length > 80)
                path = path.Substring(0, 80);

            return $"{numero:D2}_{path}.txt";
        }

        private string NormalizarUrl(Uri uri)
        {
            var builder = new UriBuilder(uri)
            {
                Fragment = string.Empty
            };

            builder.Path = NormalizarPathCanonico(builder.Path);

            return builder.Uri.ToString().TrimEnd('/');
        }

        private static string NormalizarPathCanonico(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "/";

            string normalizado = path.Replace('\\', '/');

            string[] documentosPorDefecto =
            {
                "/index.php",
                "/index.html",
                "/index.htm",
                "/default.aspx",
                "/default.html",
                "/default.htm"
            };

            foreach (string documento in documentosPorDefecto)
            {
                if (normalizado.EndsWith(documento, StringComparison.OrdinalIgnoreCase))
                {
                    normalizado = normalizado.Substring(0, normalizado.Length - documento.Length);
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(normalizado))
                return "/";

            return normalizado;
        }

        public string GenerarNombreCarpetaDominio(Uri uri)
        {
            string nombre = uri.Host.Replace(".", "_");
            foreach (char c in Path.GetInvalidFileNameChars())
                nombre = nombre.Replace(c, '_');

            return nombre;
        }

        private static string ExtraerTitulo(string html)
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var titleNode = doc.DocumentNode.SelectSingleNode("//title");
                return titleNode?.InnerText?.Trim() ?? string.Empty;
            }
            catch { return string.Empty; }
        }

        private static string InferirNombreJob(string carpetaBase)
        {
            // carpetaBase: "C:\...\App_Data\p13\izuran_blogspot_com\"
            // job = grandparent dir name = "p13"
            string ruta = carpetaBase.TrimEnd('\\', '/');
            string parent = Path.GetDirectoryName(ruta) ?? ruta;
            return Path.GetFileName(parent) ?? "unknown";
        }

        private sealed class BloqueContenido
        {
            public string Titulo { get; set; }
            public int NivelTitulo { get; set; }
            public List<string> Fragmentos { get; } = new List<string>();
        }

        private sealed class WebFormsPageState
        {
            public string Html { get; set; }
            public Uri PageUri { get; set; }
            public Uri FormAction { get; set; }
            public Dictionary<string, string> HiddenFields { get; set; }
        }

        private sealed class CatalogoListadoItem
        {
            public string Title { get; set; }
            public string EventTarget { get; set; }
        }

        private void GuardarDebugExtraccion(
            string carpetaBase,
            Uri uri,
            string htmlCrudo,
            string textoPreFiltros,
            string textoFinal,
            Quality calidad)
        {
            try
            {
                string debugFolder = Path.Combine(carpetaBase, "debug_raw_html");
                Directory.CreateDirectory(debugFolder);

                string baseName = GenerarNombreSeguro(uri, 0).Replace(".txt", string.Empty);
                string qualityTag = QualityScorer.ToLabel(calidad);

                string htmlPath = Path.Combine(debugFolder, $"{baseName}.{qualityTag}.html");
                string prePath = Path.Combine(debugFolder, $"{baseName}.{qualityTag}.pre.txt");
                string finalPath = Path.Combine(debugFolder, $"{baseName}.{qualityTag}.final.txt");

                File.WriteAllText(htmlPath, htmlCrudo ?? string.Empty, new UTF8Encoding(false));
                File.WriteAllText(prePath, (textoPreFiltros ?? string.Empty).TrimStart('\uFEFF'), new UTF8Encoding(false));
                File.WriteAllText(finalPath, (textoFinal ?? string.Empty).TrimStart('\uFEFF'), new UTF8Encoding(false));
            }
            catch
            {
            }
        }
    }
}
