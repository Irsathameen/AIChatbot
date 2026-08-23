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

            var chunks = await _db.DocumentChunks
                .OrderBy(c => c.Embedding.CosineDistance(targetVector))
                .Take(topK)
                .ToListAsync();

            var sources = new List<RagSource>();
            var contextBuilder = new StringBuilder();
            foreach (var c in chunks)
            {
                // Use CosineDistance available via Pgvector.EntityFrameworkCore
                var dist = c.Embedding.CosineDistance(targetVector);
                var score = 1.0f - (float)dist; // approximate similarity for display
                var snippet = c.Content?.Length > 400 ? c.Content.Substring(0, 400) : c.Content;
                sources.Add(new RagSource { Id = c.Id.ToString(), Score = score, Snippet = snippet });
                contextBuilder.AppendLine($"Source: {c.Id}\n{snippet}\n---\n");
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
