using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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

        private static readonly string[] _nodosBasura =
        {
            "script", "style", "noscript", "nav", "header", "footer", "aside"
        };

        private static readonly string[] _nodosInteractivos =
        {
            "input", "button", "select", "textarea", "option"
        };

        private static readonly string[] _nodosUtiles =
        {
            "h1", "h2", "h3", "h4", "h5", "h6", "p", "li", "blockquote", "dt", "dd"
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

        public CrawlerService()
            : this(CrawlerSettings.Load())
        {
        }

        public CrawlerService(CrawlerSettings settings)
        {
            settings = settings ?? CrawlerSettings.Load();
            _requestDelayMs = settings.RequestDelayMs;
            _httpTimeout = TimeSpan.FromSeconds(settings.HttpTimeoutSeconds);
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
            var cola = new Queue<Tuple<Uri, int>>();
            cola.Enqueue(Tuple.Create(startUri, 0));

            int contador = 0;
            string primerError = null;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
            };

            using (var client = new HttpClient(handler))
            {
                client.Timeout = _httpTimeout;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("TFG-Crawler/1.0");

                while (cola.Count > 0 && contador < maxPaginas)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var item = cola.Dequeue();
                    var currentUri = item.Item1;
                    int depth = item.Item2;

                    string currentUrl = NormalizarUrl(currentUri);
                    if (visitadas.Contains(currentUrl))
                        continue;

                    visitadas.Add(currentUrl);

                    string html;
                    try
                    {
                        html = await client.GetStringAsync(currentUri).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (primerError == null)
                            primerError = $"{currentUri}: {ex.GetBaseException().Message}";

                        await EsperarEntrePeticionesAsync(cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (depth < maxDepth)
                    {
                        var enlaces = ExtraerEnlacesInternos(html, currentUri, startUri.Host);
                        foreach (var enlace in enlaces)
                        {
                            string enlaceNormalizado = NormalizarUrl(enlace);
                            if (!visitadas.Contains(enlaceNormalizado))
                                cola.Enqueue(Tuple.Create(enlace, depth + 1));
                        }
                    }

                    string textoLimpio = ExtraerTextoLimpio(html, currentUri.ToString());
                    if (string.IsNullOrWhiteSpace(textoLimpio))
                    {
                        await EsperarEntrePeticionesAsync(cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    string nombreArchivo = GenerarNombreSeguro(currentUri, contador + 1);
                    string rutaArchivo = Path.Combine(carpetaBase, nombreArchivo);

                    File.WriteAllText(rutaArchivo, textoLimpio, Encoding.UTF8);
                    contador++;

                    await EsperarEntrePeticionesAsync(cancellationToken).ConfigureAwait(false);
                }
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
            if (EsPaginaDeBajoValor(debugUrl))
                return string.Empty;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            EliminarNodos(doc, _nodosBasura);
            EliminarNodos(doc, _nodosInteractivos);
            EliminarNodosOcultosODecorativos(doc);
            EliminarNodosPorAtributosDeRuido(doc);

            HtmlNode contenido = SeleccionarContenidoPrincipal(doc);
            var bloques = ExtraerBloquesSemanticos(contenido);

            if (bloques.Count == 0)
            {
                var body = doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;
                bloques = ExtraerBloquesSemanticos(body);
            }

            bloques = DepurarBloquesParaRag(bloques);
            return string.Join(Environment.NewLine + Environment.NewLine, bloques);
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

        private List<string> ExtraerBloquesSemanticos(HtmlNode contenedor)
        {
            var bloques = new List<BloqueContenido>();
            var nodosUtiles = contenedor.SelectNodes(
                ".//h1|.//h2|.//h3|.//h4|.//h5|.//h6|.//p|.//li|.//blockquote|.//dt|.//dd");

            if (nodosUtiles == null || nodosUtiles.Count == 0)
            {
                string textoPlano = NormalizarTexto(HtmlEntity.DeEntitize(contenedor.InnerText));
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

            return bloques
                .Select(RenderizarBloque)
                .Where(EsBloqueIndexable)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
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
            var candidatosDirectos = new[]
            {
                doc.DocumentNode.SelectSingleNode("//main"),
                doc.DocumentNode.SelectSingleNode("//article"),
                doc.DocumentNode.SelectSingleNode("//div[@id='content']"),
                doc.DocumentNode.SelectSingleNode("//div[@id='main']"),
                doc.DocumentNode.SelectSingleNode("//div[contains(@class,'content')]")
            }.Where(n => n != null).ToList();

            if (candidatosDirectos.Any())
                return candidatosDirectos.OrderByDescending(CalcularPuntuacionContenido).First();

            var candidatos = doc.DocumentNode.SelectNodes("//body|//form|//section|//article|//main|//div");
            if (candidatos == null || candidatos.Count == 0)
                return doc.DocumentNode;

            var mejor = candidatos
                .Where(TieneTextoVisibleSuficiente)
                .OrderByDescending(CalcularPuntuacionContenido)
                .FirstOrDefault();

            return mejor ?? doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;
        }

        private bool TieneTextoVisibleSuficiente(HtmlNode nodo)
        {
            string texto = HtmlEntity.DeEntitize(nodo.InnerText ?? string.Empty);
            texto = Regex.Replace(texto, @"\s+", " ").Trim();
            return texto.Length >= 120;
        }

        private int CalcularPuntuacionContenido(HtmlNode nodo)
        {
            var nodosUtiles = nodo.SelectNodes(".//h1|.//h2|.//h3|.//h4|.//h5|.//h6|.//p|.//li|.//blockquote|.//dt|.//dd");
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
                if (EsNodoDeRuido(nodo))
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

            return builder.Uri.ToString().TrimEnd('/');
        }

        public string GenerarNombreCarpetaDominio(Uri uri)
        {
            string nombre = uri.Host.Replace(".", "_");
            foreach (char c in Path.GetInvalidFileNameChars())
                nombre = nombre.Replace(c, '_');

            return nombre;
        }

        private sealed class BloqueContenido
        {
            public string Titulo { get; set; }
            public int NivelTitulo { get; set; }
            public List<string> Fragmentos { get; } = new List<string>();
        }
    }
}
