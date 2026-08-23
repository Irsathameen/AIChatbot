
using System.ComponentModel.DataAnnotations;

namespace AIData.DTO
{
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

