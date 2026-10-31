using System.Globalization;
using Npgsql;

namespace AIChatbot.Utility
{
    public class DocumentChunkRepository
    {
        private readonly string _connectionString;
        private readonly OllamaEmbeddingService _ollamaEmbeddingService;
        private readonly int _embeddingDimension;

        public DocumentChunkRepository(IConfiguration configuration, OllamaEmbeddingService ollamaEmbeddingService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Database connection string 'DefaultConnection' was not found.");

            _ollamaEmbeddingService = ollamaEmbeddingService;
            _embeddingDimension = int.TryParse(configuration["Ollama:EmbeddingDimensions"], out var value) ? value : 1536;
        }

        public async Task<int> SavePdfChunksAsync(Guid userId, string pdfFileName, string pdfText)
        {
            var chunks = SplitTextIntoChunks(pdfText, 800, 120);
            var savedCount = 0;

            foreach (var chunk in chunks)
            {
                var embedding = await _ollamaEmbeddingService.GetEmbeddingAsync(chunk);
                if (embedding.Length != _embeddingDimension)
                {
                    throw new InvalidOperationException($"Embedding length mismatch. Expected {_embeddingDimension} dimensions but received {embedding.Length}.");
                }

                await SaveChunkAsync(chunk, new
                {
                    sourceFile = pdfFileName,
                    chunkSize = chunk.Length
                }, embedding, userId);

                savedCount++;
            }

            return savedCount;
        }

        public async Task SaveChunkAsync(string content, object metadata, float[] embedding, Guid userId)
        {
            if (embedding.Length != _embeddingDimension)
            {
                throw new InvalidOperationException($"Embedding length mismatch. Expected {_embeddingDimension} dimensions but received {embedding.Length}.");
            }

            var metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
            var vectorValue = ToVectorString(embedding);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT INTO public.documentchunks (id, content, metadata, embedding, owner_id)
                VALUES (@id, @content, @metadata::jsonb, @embedding::vector, @ownerId)
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", Guid.NewGuid());
            command.Parameters.AddWithValue("content", content);
            command.Parameters.AddWithValue("metadata", metadataJson);
            command.Parameters.AddWithValue("embedding", vectorValue);
            command.Parameters.AddWithValue("ownerId", userId);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<DocumentChunkMatch>> SearchAsync(Guid userId, string query, int limit = 5)
        {
            var queryEmbedding = await _ollamaEmbeddingService.GetEmbeddingAsync(query);
            var vectorValue = ToVectorString(queryEmbedding);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT id, content, metadata, 1 - (embedding <=> @queryEmbedding::vector) AS similarity
                FROM public.documentchunks
                WHERE owner_id = @ownerId
                ORDER BY embedding <=> @queryEmbedding::vector
                LIMIT @limit
            ";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("queryEmbedding", vectorValue);
            command.Parameters.AddWithValue("ownerId", userId);
            command.Parameters.AddWithValue("limit", limit);

            var results = new List<DocumentChunkMatch>();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var metadata = reader.IsDBNull(2) ? null : reader.GetString(2);
                var similarity = reader.GetDouble(3);

                results.Add(new DocumentChunkMatch
                {
                    Id = reader.GetGuid(0),
                    Content = reader.GetString(1),
                    Metadata = metadata,
                    Score = (float)similarity
                });
            }

            return results;
        }

        private static string ToVectorString(float[] embedding)
        {
            return "[" + string.Join(",", embedding.Select(v => v.ToString(CultureInfo.InvariantCulture))) + "]";
        }

        private static List<string> SplitTextIntoChunks(string text, int chunkSize, int overlap)
        {
            var chunks = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return chunks;
            }

            var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            for (var index = 0; index < words.Length; index += chunkSize - overlap)
            {
                var slice = words.Skip(index).Take(chunkSize).ToArray();
                if (slice.Length == 0)
                {
                    break;
                }

                chunks.Add(string.Join(" ", slice));

                if (index + chunkSize >= words.Length)
                {
                    break;
                }
            }

            return chunks;
        }
    }

    public class DocumentChunkMatch
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Metadata { get; set; }
        public float Score { get; set; }
    }
}
