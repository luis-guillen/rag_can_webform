using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace rag_can_aspx.Services.Jobs
{
    /// <summary>
    /// Punto de extension para empujar los chunks a un indice vectorial (Qdrant via rag_can_python).
    /// En el NIVEL 1 NO se invoca contra Qdrant: el IndexJob usa <see cref="NullVectorIndexSink"/>.
    /// Dejar esta interfaz lista permite conectar el upsert real mas adelante sin tocar el IndexJob.
    /// </summary>
    public interface IVectorIndexSink
    {
        bool IsEnabled { get; }

        Task UpsertAsync(PageMetadataDocument doc, IEnumerable<string> chunks, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Implementacion por defecto: no hace nada (solo metadata + estado).
    /// </summary>
    public sealed class NullVectorIndexSink : IVectorIndexSink
    {
        public bool IsEnabled
        {
            get { return false; }
        }

        public Task UpsertAsync(PageMetadataDocument doc, IEnumerable<string> chunks, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
