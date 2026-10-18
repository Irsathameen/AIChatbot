using System.ComponentModel.DataAnnotations.Schema;

namespace AI.Data.Model
{
    public class DocumentChunk
    {
        public Guid id { get; set; }
        public string content { get; set; }
        // Store embedding as a float array; for SQL Server we persist this
        // as varbinary (converted in the DbContext).
        public float[] embedding { get; set; }
    }

}
