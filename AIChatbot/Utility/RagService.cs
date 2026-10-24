using AIChatbot.DTO;
using System.Text.Json;

namespace AIChatbot.Utility
{
    public class RagService
    {
        private readonly DocumentChunkRepository _repository;
        private readonly OllamaService _ollamaService;

        public RagService(DocumentChunkRepository repository, OllamaService ollamaService)
        {
            _repository = repository;
            _ollamaService = ollamaService;
        }

        public async Task<int> IndexPdfAsync(string filePath, string fileName)
        {
            var pdfText = await PdfTextExtractor.ExtractTextFromFileAsync(filePath);
            return await _repository.SavePdfChunksAsync(fileName, pdfText);
        }

        public async Task<RagResponse> AskAsync(string question)
        {
            var matches = await _repository.SearchAsync(question, 5);
            var context = string.Join("\n\n---\n\n", matches.Select(m => m.Content));

            var prompt = $@"
You are a helpful assistant using the uploaded document context only.
Answer the user's question based on the context below. If the answer is not present in the context, say that the information was not found in the uploaded document.

Context:
{context}

User question:
{question}
";

            var rawResponse = await _ollamaService.ChatAsync(prompt);
            var response = JsonSerializer.Deserialize<ChatResponse>(rawResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var answer = response?.Message?.Content ?? "No answer returned from the model.";

            return new RagResponse
            {
                Answer = answer,
                Sources = matches
                    .Select(m => new RagSource
                    {
                        Id = m.Id.ToString(),
                        Score = m.Score,
                        Snippet = m.Content.Length > 300 ? m.Content.Substring(0, 300) + "..." : m.Content
                    })
                    .ToList()
            };
        }
    }
}
