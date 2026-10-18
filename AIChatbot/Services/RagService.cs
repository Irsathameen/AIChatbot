using System.Text;
using System.Linq;
using System.Collections.Generic;
using AIChatbot.Utility;
using Microsoft.EntityFrameworkCore;
using System;
using AIChatbot.DTO;

namespace AIChatbot.Services
{
    public class RagService
    {
        private readonly ChatbotDbContext _db;
        private readonly IEmbeddingService _embedding;
        private readonly OllamaService _ollama;

        public RagService(ChatbotDbContext db, IEmbeddingService embedding, OllamaService ollama)
        {
            _db = db;
            _embedding = embedding;
            _ollama = ollama;
        }

        public async Task<RagResponse> QueryAsync(string userPrompt, int topK = 3)
        {
            var queryVec = await _embedding.GetEmbeddingAsync(userPrompt);

            // Load candidate chunks and compute cosine similarity in-memory.
            var allChunks = await _db.documentchunk
                .Select(c => new { Chunk = c, c.embedding })
                .ToListAsync();

            float CosineSimilarity(float[] a, float[] b)
            {
                if (a == null || b == null) return 0f;
                var len = Math.Min(a.Length, b.Length);
                double dot = 0, na = 0, nb = 0;
                for (int i = 0; i < len; i++)
                {
                    dot += a[i] * b[i];
                    na += a[i] * a[i];
                    nb += b[i] * b[i];
                }
                if (na == 0 || nb == 0) return 0f;
                return (float)(dot / (Math.Sqrt(na) * Math.Sqrt(nb)));
            }

            var chunks = allChunks
                .Select(x => new { Chunk = x.Chunk, Score = CosineSimilarity(queryVec, x.embedding) })
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .Select(x => new { Chunk = x.Chunk, Distance = 1.0f - x.Score })
                .ToList();

            var sources = new List<RagSource>();
            var contextBuilder = new StringBuilder();
            foreach (var item in chunks)
            {
                var score = 1.0f - (float)item.Distance;
                var snippet = item.Chunk.content?.Length > 400
                    ? item.Chunk.content.Substring(0, 400)
                    : item.Chunk.content;

                sources.Add(new RagSource
                {
                    Id = item.Chunk.id.ToString(),
                    Score = score,
                    Snippet = snippet
                });

                contextBuilder.AppendLine($"Source: {item.Chunk.id}\n{snippet}\n---\n");
            }
            var promptWithContext = $"Use the following retrieved documents as context when answering.\n\n{contextBuilder}\nUser: {userPrompt}";

            var llmRaw = await _ollama.ChatAsync(promptWithContext);
            // Try to extract content if the OllamaService returns ChatResponse-shaped JSON
            string answer = llmRaw;
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<AIData.DTO.ChatResponse>(llmRaw);
                if (parsed?.Message?.Content != null)
                {
                    answer = parsed.Message.Content;
                }
            }
            catch
            {
                // leave raw
            }

            return new RagResponse
            {
                Answer = answer,
                Sources = sources
            };
        }
    }
}
