using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace rag_can_aspx.Services.Jobs
{
    /// <summary>
    /// Trocea texto limpio en fragmentos ("chunks"). Sirve para:
    ///  - dar un numero real de chunks al estado del indexado, y
    ///  - ser la base del futuro upsert a Qdrant (ver IVectorIndexSink).
    /// El troceo respeta limites de parrafo cuando es posible y nunca parte
    /// por debajo del tamano objetivo configurado.
    /// </summary>
    public static class Chunker
    {
        private const int DefaultChunkChars = 1000;

        public static int ChunkSizeFromConfig()
        {
            string raw = ConfigurationManager.AppSettings["Index:ChunkSize"];
            int value;
            if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out value) && value >= 200 && value <= 8000)
                return value;
            return DefaultChunkChars;
        }

        public static int CountChunks(string text)
        {
            return Split(text, ChunkSizeFromConfig()).Count;
        }

        public static List<string> Split(string text, int chunkChars)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return result;

            if (chunkChars < 200)
                chunkChars = DefaultChunkChars;

            string[] paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            var current = new StringBuilder();
            foreach (string raw in paragraphs)
            {
                string paragraph = (raw ?? string.Empty).Trim();
                if (paragraph.Length == 0)
                    continue;

                // Un parrafo enorme se parte por longitud.
                if (paragraph.Length > chunkChars)
                {
                    FlushBuffer(current, result);
                    for (int i = 0; i < paragraph.Length; i += chunkChars)
                    {
                        int len = Math.Min(chunkChars, paragraph.Length - i);
                        result.Add(paragraph.Substring(i, len).Trim());
                    }
                    continue;
                }

                if (current.Length > 0 && current.Length + paragraph.Length + 2 > chunkChars)
                    FlushBuffer(current, result);

                if (current.Length > 0)
                    current.Append("\n\n");
                current.Append(paragraph);
            }

            FlushBuffer(current, result);
            return result.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        }

        private static void FlushBuffer(StringBuilder buffer, List<string> result)
        {
            if (buffer.Length == 0)
                return;
            result.Add(buffer.ToString().Trim());
            buffer.Clear();
        }
    }
}
