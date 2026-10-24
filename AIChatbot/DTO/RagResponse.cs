using System.Collections.Generic;

namespace AIChatbot.DTO
{
    public class RagResponse
    {
        public string Answer { get; set; } = string.Empty;
        public List<RagSource> Sources { get; set; } = new();
    }

    public class RagSource
    {
        public string Id { get; set; } = string.Empty;
        public float Score { get; set; }
        public string Snippet { get; set; } = string.Empty;
    }
}
