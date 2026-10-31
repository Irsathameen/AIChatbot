
using AIChatbot.DTO;
using AIChatbot.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace AIChatbot.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AiController : ControllerBase
    {
        private readonly OllamaService _ollamaService;
        private readonly RagService _ragService;
        private readonly ILogger<AiController> _logger;

        public AiController(OllamaService ollamaService, RagService ragService, ILogger<AiController> logger)
        {
            _ollamaService = ollamaService;
            _ragService = ragService;
            _logger = logger;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest chatRequest)
        {
            if (chatRequest == null || string.IsNullOrWhiteSpace(chatRequest.Prompt))
            {
                return BadRequest(new { error = "Prompt is required." });
            }

            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            if (chatRequest.IsRAG)
            {
                var ragResponse = await _ragService.AskAsync(userId, chatRequest.Prompt);
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

        private const long MaxPdfSize = 25 * 1024 * 1024;

        [HttpPost("upload-pdf")]
        [RequestSizeLimit(MaxPdfSize + 64 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxPdfSize + 64 * 1024)]
        public async Task<IActionResult> UploadPdf(IFormFile file)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Please upload a valid PDF file." });
            }

            if (file.Length > MaxPdfSize)
            {
                return BadRequest(new { error = "PDF files must be 25 MB or smaller." });
            }

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Only PDF files are supported." });
            }

            var uploadsFolder = Path.Combine(Path.GetTempPath(), "AIChatbot", "RAGUploads", userId.ToString("N"));
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, $"{Guid.NewGuid():N}.pdf");
            try
            {
                await using (var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write))
                {
                    await file.CopyToAsync(stream);
                }

                var chunkCount = await _ragService.IndexPdfAsync(userId, filePath, Path.GetFileName(file.FileName));
                if (chunkCount == 0)
                {
                    return BadRequest(new { error = "This PDF has no extractable text to index." });
                }

                return Ok(new
                {
                    message = "PDF uploaded and indexed successfully.",
                    fileName = Path.GetFileName(file.FileName),
                    chunkCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to index PDF for user {UserId}.", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "The PDF could not be indexed. Please try again." });
            }
            finally
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
        }

        private bool TryGetUserId(out Guid userId) =>
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}






