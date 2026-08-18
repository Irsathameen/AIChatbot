using Microsoft.AspNetCore.Mvc;
using AIChatbot.Utility;
namespace AIChatbot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiController(OllamaService ollamaService) : ControllerBase
    {
        private readonly OllamaService _ollamaService = ollamaService;

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] string prompt)
        {
            var result = await _ollamaService.ChatAsync(prompt);
            return Ok(result);
        }


    }

}





