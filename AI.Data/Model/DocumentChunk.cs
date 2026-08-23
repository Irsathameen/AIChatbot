using Pgvector;

namespace AI.Data.Model
{
    public class DocumentChunk
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public Vector Embedding { get; set; } // Map to pgvector data type
    }

}
