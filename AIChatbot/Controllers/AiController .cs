using AI.Data.Model;
using AIChatbot.Utility;
using AIData.DTO;
using Microsoft.AspNetCore.Mvc;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AIChatbot.Services;
using AIChatbot.DTO;
using System.Text;
using System.Text.Json;
namespace AIChatbot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly RagService _ragService;
        private readonly OllamaService _ollamaService;

        public AiController(RagService ragService, OllamaService ollamaService)
        {
            _ragService = ragService;
            _ollamaService = ollamaService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest chatRequest)
        {
            if(!chatRequest.IsRAG && !string.IsNullOrWhiteSpace(chatRequest.Prompt))
            {
                var result = await _ollamaService.ChatAsync(chatRequest?.Prompt);
                var res = JsonSerializer.Deserialize<ChatResponse>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Normalize model response
                var content = res?.Message?.Content;

                if (string.IsNullOrWhiteSpace(content))
                {
                    // Return a JSON error body for not found
                    return NotFound(new { error = "no content returned from model" });
                }

                // Return a consistent JSON object for non-RAG responses
                return Ok(new
                {
                    answer = content,
                    isRag = false,
                    sources = Array.Empty<object>()
                });
            }
            if (chatRequest == null || string.IsNullOrWhiteSpace(chatRequest.Prompt))
                return BadRequest(new { error = "prompt is required" });

            var ragResult = await _ragService.QueryAsync(chatRequest.Prompt, topK: 3);
            // Return formatted JSON with answer and sources
            return Ok(ragResult);
        }

        public async Task<List<DocumentChunk>> RetrieveRelevantChunksAsync(ChatbotDbContext context, float[] queryEmbedding, int limit = 3)
        {
            var targetVector = new Vector(queryEmbedding);

            // Order items relative to the shortest cosine distance to find the closest context
            return await context.DocumentChunks
                .OrderBy(c => c.Embedding.CosineDistance(targetVector))
                .Take(limit)
                .ToListAsync();
        }

    }
}









