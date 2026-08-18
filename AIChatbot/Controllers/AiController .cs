using AIChatbot.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using AIChatbot.DTO;
namespace AIChatbot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiController(OllamaService ollamaService) : ControllerBase
    {
        private readonly OllamaService _ollamaService = ollamaService;

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest chatRequest)
        {
            var result = await _ollamaService.ChatAsync(chatRequest?.prompt);
            var res = JsonSerializer.Deserialize<ChatResponse>(result); 
            var content = res?.message?.content ?? string.Empty;
            if (content == null) return NotFound(); return Ok(content);
        }


    }

}





