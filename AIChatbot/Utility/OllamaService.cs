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
            _httpClient.BaseAddress = new Uri(_config["Ollama:Host"] ?? "https://ollama.com");

            var apiKey = config["Ollama:API_Key"];
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }
        }

        public async Task<string> ChatAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be empty.", nameof(prompt));
            }

            var requestBody = new
            {
                model = _config["Ollama:ModelId"] ?? "gpt-oss:120b",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                stream = false
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/chat", content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
