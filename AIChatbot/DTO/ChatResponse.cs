
using System.ComponentModel.DataAnnotations;

namespace AIChatbot.DTO
{
    public class ChatResponse 
    { public Message? message { get; set; } }
    public class Message 
    { 
        public string? role { get; set; }
        public string? content { get; set; } 
    }

    public class ChatRequest
    {
        [Required]
        public string prompt { get; set; }
    }

}

