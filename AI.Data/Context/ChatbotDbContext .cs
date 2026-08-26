using AI.Data.Model;
using Microsoft.EntityFrameworkCore;
using Pgvector;

public class ChatbotDbContext : DbContext
{
    public ChatbotDbContext(DbContextOptions<ChatbotDbContext> options) : base(options) { }

    public DbSet<DocumentChunk> documentchunks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tells EF Core to load the vector extension definitions
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            // Explicitly map your embedding property as a specific vector size (e.g., 1536 for OpenAI)
            entity.Property(e => e.embedding).HasColumnType("vector(1536)");
        });
    }
}
