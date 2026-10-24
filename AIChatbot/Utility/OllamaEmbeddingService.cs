using System.Globalization;
using System.Text.Json;

namespace AIChatbot.Utility
{
    public class OllamaEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public OllamaEmbeddingService(HttpClient httpClient, IConfiguration config)
        {
            _config = config;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(_config["Ollama:EmbeddingHost"] ?? "http://localhost:11434");
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Embedding text cannot be empty.", nameof(text));
            }

            var model = _config["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
            var payload = new { model, input = text };

            var response = await _httpClient.PostAsJsonAsync("/api/embed", payload);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Embedding request failed (/api/embed): {response.StatusCode}. Details: {responseText}");
            }

            using var document = JsonDocument.Parse(responseText);
            if (TryGetEmbedding(document.RootElement, out var embedding))
            {
                return embedding;
            }

            throw new InvalidOperationException("The Ollama embedding API did not return a usable embedding.");
        }

        private static bool TryGetEmbedding(JsonElement root, out float[] embedding)
        {
            embedding = Array.Empty<float>();

            if (root.TryGetProperty("embedding", out var embeddingValue) && embeddingValue.ValueKind == JsonValueKind.Array)
            {
                embedding = ParseEmbedding(embeddingValue);
                return embedding.Length > 0;
            }

            if (root.TryGetProperty("embeddings", out var embeddingsValue) && embeddingsValue.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in embeddingsValue.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Array)
                    {
                        embedding = ParseEmbedding(item);
                        return embedding.Length > 0;
                    }
                }
            }

            return false;
        }

        private static float[] ParseEmbedding(JsonElement arrayElement)
        {
            var values = new List<float>();
            foreach (var item in arrayElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Number && item.TryGetSingle(out var numberValue))
                {
                    values.Add(numberValue);
                }
                else if (item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out var doubleValue))
                {
                    values.Add((float)doubleValue);
                }
                else if (item.ValueKind == JsonValueKind.String && float.TryParse(item.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                {
                    values.Add(parsed);
                }
            }

            return values.ToArray();
        }
    }
}
