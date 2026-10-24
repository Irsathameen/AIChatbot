
using AIChatbot.DTO;
using AIChatbot.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AIChatbot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly OllamaService _ollamaService;
        private readonly RagService _ragService;

        public AiController(OllamaService ollamaService, RagService ragService)
        {
            _ollamaService = ollamaService;
            _ragService = ragService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest chatRequest)
        {
            if (chatRequest == null || string.IsNullOrWhiteSpace(chatRequest.Prompt))
            {
                return BadRequest(new { error = "Prompt is required." });
            }

            if (chatRequest.IsRAG)
            {
                var ragResponse = await _ragService.AskAsync(chatRequest.Prompt);
                return Ok(new
                {
                    answer = ragResponse.Answer,
                    isRag = true,
                    sources = ragResponse.Sources
                });
            }

            var result = await _ollamaService.ChatAsync(chatRequest.Prompt);
            var response = JsonSerializer.Deserialize<ChatResponse>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var content = response?.Message?.Content;

            if (string.IsNullOrWhiteSpace(content))
            {
                return NotFound(new { error = "No content returned from model." });
            }

            return Ok(new
            {
                answer = content,
                isRag = false,
                sources = Array.Empty<object>()
            });
        }

        [HttpPost("upload-pdf")]
        public async Task<IActionResult> UploadPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Please upload a valid PDF file." });
            }

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Only PDF files are supported." });
            }

            var uploadsFolder = Path.Combine(AppContext.BaseDirectory, "RAGUploads");
            Directory.CreateDirectory(uploadsFolder);

            var safeFileName = $"{Guid.NewGuid():N}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, safeFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await file.CopyToAsync(stream);
            }

            try
            {
                var chunkCount = await _ragService.IndexPdfAsync(filePath, file.FileName);
                return Ok(new
                {
                    message = "PDF uploaded and indexed successfully.",
                    fileName = file.FileName,
                    chunkCount,
                    source = filePath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }
    }
}









