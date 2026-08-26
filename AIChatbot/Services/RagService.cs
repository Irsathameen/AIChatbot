using System.Text;
using System.Linq;
using System.Collections.Generic;
using AIChatbot.Utility;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
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
            var targetVector = new Vector(queryVec);

            var chunks = await _db.documentchunks
            .OrderBy(c => c.embedding.CosineDistance(targetVector))
            .Take(topK)
            .Select(c => new
            {    
            Chunk = c,
            Distance = c.embedding.CosineDistance(targetVector)
            })
            .ToListAsync();

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
