using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using AI.Data.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pgvector;

namespace AIChatbot.Services
{
    public class PdfIngestor
    {
        private readonly ChatbotDbContext _db;
        private readonly IEmbeddingService _emb;
        private readonly int _chunkSize;
        private readonly int _chunkOverlap;

        public PdfIngestor(ChatbotDbContext db, IEmbeddingService emb, int chunkSize = 1500, int chunkOverlap = 200)
        {
            _db = db;
            _emb = emb;
            _chunkSize = chunkSize;
            _chunkOverlap = chunkOverlap;
        }
        public async Task<int> IngestPdfAsync(string filePath, CancellationToken cancellation = default)
        {
            // Process the PDF page-by-page to avoid holding the entire document text in memory.
            var saved = 0;
            const int batchSize = 16; // limit concurrent embedding requests and DB tracking

            using (var reader = new PdfReader(filePath))
            using (var pdf = new PdfDocument(reader))
            {
                var numPages = pdf.GetNumberOfPages();
                for (int p = 1; p <= numPages; p++)
                {
                    cancellation.ThrowIfCancellationRequested();

                    var page = pdf.GetPage(p);
                    var strategy = new SimpleTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(page, strategy)?.Trim();
                    if (string.IsNullOrWhiteSpace(pageText))
                        continue;

                    // Stream chunks and process in bounded batches to avoid large allocations
                    var chunkStream = ChunkText(pageText, _chunkSize, _chunkOverlap).GetEnumerator();
                    var batch = new List<string>(batchSize);

                    async Task ProcessBatchAsync(List<string> items)
                    {
                        if (items.Count == 0) return;

                        // Generate embeddings for this batch
                        var embeddings = await Task.WhenAll(items.Select(c => _emb.GetEmbeddingAsync(c)));

                        for (int j = 0; j < items.Count; j++)
                        {
                            var content = items[j];
                            var vecFloats = embeddings[j];
                            if (vecFloats == null || vecFloats.Length == 0)
                                continue;

                            var pgVector = new Vector(vecFloats);
                            var docChunk = new DocumentChunk
                            {
                                id= Guid.NewGuid(),
                                content = content,
                                embedding = pgVector
                            };

                            _db.documentchunks.Add(docChunk);
                            saved++;
                        }

                        await _db.SaveChangesAsync(cancellation);
                        // Clear tracked entities so EF Core doesn't keep growing memory
                        _db.ChangeTracker.Clear();
                    }

                    while (true)
                    {
                        cancellation.ThrowIfCancellationRequested();

                        if (!chunkStream.MoveNext()) break;
                        batch.Add(chunkStream.Current);

                        if (batch.Count >= batchSize)
                        {
                            await ProcessBatchAsync(batch);
                            batch.Clear();
                        }
                    }

                    if (batch.Count > 0)
                    {
                        await ProcessBatchAsync(batch);
                        batch.Clear();
                    }
                }
            }

            return saved;
        }

        private static IEnumerable<string> ChunkText(string text, int maxLen, int overlap)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;

            int start = 0;
            var len = text.Length;

            while (start < len)
            {
                var take = Math.Min(maxLen, len - start);
                var chunk = text.Substring(start, take).Trim();

                if (take == maxLen)
                {
                    var lastPeriod = chunk.LastIndexOfAny(new[] { '.', '\n', '?', '!' });
                    if (lastPeriod > maxLen / 2)
                    {
                        chunk = chunk.Substring(0, lastPeriod + 1).Trim();
                        take = lastPeriod + 1;
                    }
                }

                if (!string.IsNullOrWhiteSpace(chunk))
                    yield return chunk;

                // Advance using overlap but ensure forward progress
                var advance = Math.Max(1, take - overlap);
                start += advance;
            }
        }
    }
}
