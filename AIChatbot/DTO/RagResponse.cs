using System.Collections.Generic;

namespace AIChatbot.DTO
{
    public class RagResponse
    {
        public string Answer { get; set; }
        public List<RagSource> Sources { get; set; } = new();
    }

    public class RagSource
    {
        public string Id { get; set; }
        public float Score { get; set; }
        public string Snippet { get; set; }
    }
}
