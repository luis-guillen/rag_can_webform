using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace rag_can_aspx.Services.Jobs
{
    /// <summary>
    /// Job de indexado incremental. Procesa SOLO los documentos con needs_index=true
    /// (los que el crawler marco como nuevos/cambiados). En el NIVEL 1 el "indexado" consiste
    /// en refrescar metadata, calcular el numero de chunks y registrar last_indexed_at; el
    /// upsert real a Qdrant queda preparado via IVectorIndexSink pero NO se invoca por defecto.
    /// </summary>
    public static class IndexJob
    {
        // Sustituible por una implementacion real (RagPythonVectorIndexSink) cuando se conecte Qdrant.
        public static IVectorIndexSink Sink = new NullVectorIndexSink();

        public static async Task RunAsync(CancellationToken cancellationToken)
        {
            string projectRoot = JobStatusManager.ProjectRoot;
            var svc = new MetadataService(projectRoot);

            var status = new JobRunStatus
            {
                Kind = "index",
                RunId = Guid.NewGuid().ToString("N"),
                State = JobStates.Running,
                StartedAt = DateTime.UtcNow.ToString("o"),
                ProgressPercent = 0
            };
            JobStatusManager.WriteIndexStatus(status);

            List<PageMetadataDocument> all = svc.LoadAll();
            List<PageMetadataDocument> toIndex = all
                .Where(d => d != null && d.PageMetadata != null && d.PageMetadata.NeedsIndex)
                .ToList();

            status.TotalSources = toIndex.Count;
            JobStatusManager.WriteIndexStatus(status);
            JobLogger.Append(JobLogger.IndexerLog, string.Format("=== Indexado iniciado: {0} documentos pendientes ===", toIndex.Count));

            int done = 0;

            try
            {
                foreach (PageMetadataDocument doc in toIndex)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string url = doc.PageMetadata.Url;
                    status.CurrentUrl = url;
                    JobStatusManager.WriteIndexStatus(status);

                    try
                    {
                        string absolute = Path.Combine(projectRoot, doc.PageMetadata.File.Replace('/', Path.DirectorySeparatorChar));
                        if (!File.Exists(absolute))
                            throw new FileNotFoundException("No existe el .txt asociado", absolute);

                        string text = File.ReadAllText(absolute, Encoding.UTF8).TrimStart('﻿');
                        List<string> chunks = Chunker.Split(text, Chunker.ChunkSizeFromConfig());

                        if (Sink != null && Sink.IsEnabled)
                            await Sink.UpsertAsync(doc, chunks, cancellationToken).ConfigureAwait(false);

                        doc.PageMetadata.Chunks = chunks.Count;
                        doc.PageMetadata.LastIndexedAt = DateTime.UtcNow.ToString("o");
                        doc.PageMetadata.NeedsIndex = false;

                        status.ProcessedSources++;
                        JobLogger.Append(JobLogger.IndexerLog, string.Format("OK {0} ({1} chunks)", url, chunks.Count));
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        status.FailedSources++;
                        status.LastError = ex.GetBaseException().Message;
                        JobLogger.Append(JobLogger.IndexerLog, string.Format("FALLO {0}: {1}", url, ex.GetBaseException().Message));
                    }
                    finally
                    {
                        done++;
                        status.ProgressPercent = toIndex.Count == 0 ? 100 : (int)Math.Round(done * 100.0 / toIndex.Count);
                        JobStatusManager.WriteIndexStatus(status);
                    }
                }

                // Persistir cambios (sidecars + metadata.json raiz) una sola vez.
                svc.SaveAll(all);
                ActualizarRollupFuentes(all);

                status.State = cancellationToken.IsCancellationRequested ? JobStates.Stopped : JobStates.Completed;
            }
            catch (OperationCanceledException)
            {
                // Guardar lo procesado hasta el momento del stop.
                try { svc.SaveAll(all); ActualizarRollupFuentes(all); } catch { }
                status.State = JobStates.Stopped;
            }
            catch (Exception ex)
            {
                status.State = JobStates.Error;
                status.LastError = ex.GetBaseException().Message;
            }
            finally
            {
                status.CurrentUrl = null;
                status.ProgressPercent = 100;
                status.FinishedAt = DateTime.UtcNow.ToString("o");
                JobStatusManager.WriteIndexStatus(status);
                JobLogger.Append(JobLogger.IndexerLog, string.Format(
                    "=== Indexado {0}: {1} ok, {2} con fallo ===",
                    status.State, status.ProcessedSources, status.FailedSources));
            }
        }

        /// <summary>
        /// Refleja en sources_status.json el resultado del indexado por host: numero de chunks,
        /// last_indexed_at y needs_index residual.
        /// </summary>
        private static void ActualizarRollupFuentes(List<PageMetadataDocument> all)
        {
            try
            {
                SourcesStatusFile sources = JobStatusManager.ReadSources();
                if (sources == null || sources.Sources == null || sources.Sources.Count == 0)
                    return;

                var porHost = all
                    .Where(d => d != null && d.PageMetadata != null && !string.IsNullOrWhiteSpace(d.PageMetadata.Domain))
                    .GroupBy(d => d.PageMetadata.Domain, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                string now = DateTime.UtcNow.ToString("o");

                foreach (SourceStatus source in sources.Sources)
                {
                    if (string.IsNullOrWhiteSpace(source.Host))
                        continue;

                    List<PageMetadataDocument> docs;
                    if (!porHost.TryGetValue(source.Host, out docs))
                        continue;

                    source.ChunkCount = docs.Sum(d => d.PageMetadata.Chunks);
                    source.NeedsIndex = docs.Any(d => d.PageMetadata.NeedsIndex);
                    if (docs.Any(d => !string.IsNullOrWhiteSpace(d.PageMetadata.LastIndexedAt)))
                        source.LastIndexedAt = now;
                }

                JobStatusManager.WriteSources(sources);
            }
            catch
            {
            }
        }
    }
}
