using System;
using System.IO;

namespace rag_can_aspx.Services
{
    /// <summary>
    /// Utilidades para manejo de rutas portables entre MVC y Web Forms
    /// </summary>
    public class PathHelper
    {
        /// <summary>
        /// Resuelve la ruta de guardado. SIEMPRE crea carpetas dentro de App_Data por seguridad.
        /// - Si está vacía → usa App_Data/crawlings/
        /// - Si no está vacía → valida que sea solo un nombre de carpeta (sin barras ni rutas absolutas)
        ///   y la crea dentro de App_Data/
        /// </summary>
        /// <param name="appDataBase">Ruta base de App_Data (ej: Server.MapPath("~/App_Data/"))</param>
        /// <param name="carpetaPersonalizada">Nombre de subcarpeta (opcional)</param>
        /// <returns>Ruta absoluta completa donde crear carpetas</returns>
        public static string ResolverRutaCarpeta(string appDataBase, string carpetaPersonalizada)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(appDataBase))
                    throw new ArgumentException("appDataBase no puede estar vacío");

                if (string.IsNullOrWhiteSpace(carpetaPersonalizada))
                {
                    // Ruta por defecto: App_Data/crawlings/
                    string rutaPorDefecto = Path.Combine(appDataBase, "crawlings") + "\\";
                    return rutaPorDefecto;
                }

                // Limpiar y validar: solo permitir nombres de carpeta (sin rutas)
                carpetaPersonalizada = carpetaPersonalizada.Trim().Trim('/').Trim('\\');

                // Rechazar intentos de ruta absoluta o salida de App_Data
                if (carpetaPersonalizada.Contains(":") ||
                    carpetaPersonalizada.StartsWith("\\") ||
                    carpetaPersonalizada.StartsWith("/") ||
                    carpetaPersonalizada.Contains("..") ||
                    carpetaPersonalizada.Contains(@"\..\") ||
                    carpetaPersonalizada.Contains("/../"))
                {
                    throw new ArgumentException("La carpeta debe ser un nombre simple sin rutas. Ej: 'MisCrawls', 'enero', etc.");
                }

                // Crear dentro de App_Data
                string rutaFinal = Path.Combine(appDataBase, carpetaPersonalizada) + "\\";
                return rutaFinal;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch
            {
                // Fallback: usar App_Data/crawlings si hay error
                return Path.Combine(appDataBase, "crawlings") + "\\";
            }
        }

        /// <summary>
        /// Obtiene la ruta relativa al proyecto para mostrar al usuario.
        /// </summary>
        /// <param name="raizProyecto">Raíz del proyecto (ej: Server.MapPath("~"))</param>
        /// <param name="rutaAbsoluta">Ruta absoluta a convertir</param>
        /// <returns>Ruta relativa con formato [Proyecto]/... o la ruta original si no se puede determinar</returns>
        public static string ObtenerRutaRelativa(string raizProyecto, string rutaAbsoluta)
        {
            try
            {
                if (rutaAbsoluta.StartsWith(raizProyecto, StringComparison.OrdinalIgnoreCase))
                {
                    // Extraer ruta relativa
                    string relativa = rutaAbsoluta.Substring(raizProyecto.Length).Trim('\\').Trim('/');
                    return $"[Proyecto]/{relativa}";
                }
            }
            catch { }

            // Si no se puede determinar, devolver la ruta tal cual
            return rutaAbsoluta;
        }
    }
}
