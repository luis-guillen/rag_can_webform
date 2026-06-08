using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace rag_can_aspx.Services.Jobs
{
    /// <summary>
    /// Helper compartido para leer/escribir JSON de forma atomica.
    /// Reutiliza el mismo patron de escritura que MetadataService (archivo .tmp + File.Replace)
    /// para evitar dejar ficheros de estado a medio escribir si el proceso se interrumpe.
    /// </summary>
    public static class JsonFile
    {
        private static readonly object _lock = new object();

        /// <summary>
        /// Serializa <paramref name="value"/> a JSON indentado y lo escribe de forma atomica.
        /// Crea el directorio destino si no existe.
        /// </summary>
        public static void WriteAtomic(string path, object value)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            string json = JsonConvert.SerializeObject(value, Formatting.Indented);

            lock (_lock)
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                string tmp = path + ".tmp";
                File.WriteAllText(tmp, json, new UTF8Encoding(false));

                if (File.Exists(path))
                    File.Replace(tmp, path, null);
                else
                    File.Move(tmp, path);
            }
        }

        /// <summary>
        /// Lee y deserializa el fichero JSON. Si no existe o falla, devuelve <paramref name="fallback"/>.
        /// </summary>
        public static T ReadOrDefault<T>(string path, T fallback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return fallback;

                string json = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                    return fallback;

                T value = JsonConvert.DeserializeObject<T>(json);
                return value == null ? fallback : value;
            }
            catch
            {
                return fallback;
            }
        }
    }
}
