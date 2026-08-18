using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AIChatbot.Utility
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        public OllamaService(HttpClient httpClient, IConfiguration config)
        {
            _config = config;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_config["Ollama:Host"]??"");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", config?["Ollama:API_Key"]);
        }

        public async Task<string> ChatAsync(string prompt)
        {
            var requestBody = new
            {
                model = _config?["Ollama:ModelId"],
                messages = new[]
                {
                new { role = "user", content = prompt }
            },
                stream = false // set true if you want streaming
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/chat", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            return responseString;
        }
    }
}
