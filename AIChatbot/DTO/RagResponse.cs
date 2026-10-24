using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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


    public class ChatResponse
    { public Message? Message { get; set; } }
    public class Message
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    public class ChatRequest
    {
        [Required]
        public string Prompt { get; set; }

        [Required]
        public bool IsRAG { get; set; }

    }



}
