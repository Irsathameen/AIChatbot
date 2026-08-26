using Pgvector;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI.Data.Model
{
    public class DocumentChunk
    {
        public Guid id { get; set; }
        public string content { get; set; }
        // Map the embedding property to the database vector column so EF Core
        // and Pgvector can translate vector operations (CosineDistance) to SQL.
        public Vector embedding { get; set; }
    }

}
