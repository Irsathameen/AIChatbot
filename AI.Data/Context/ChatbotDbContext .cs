using AI.Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

public class ChatbotDbContext : DbContext
{
    public ChatbotDbContext(DbContextOptions<ChatbotDbContext> options) : base(options) { }

    public DbSet<DocumentChunk> documentchunk { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // For SQL Server we persist float[] embeddings as varbinary using a value converter.
        // This keeps the vector bytes compact and allows retrieving them back as float[].
        var converter = new ValueConverter<float[], byte[]>
        (
            v => v == null ? null : FloatArrayToBytes(v),
            v => v == null ? null : BytesToFloatArray(v)
        );

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.Property(e => e.embedding)
                  .HasColumnType("varbinary(max)")
                  .HasConversion(converter);
        });
    }

    private static byte[] FloatArrayToBytes(float[] floats)
    {
        if (floats == null) return null;
        var bytes = new byte[floats.Length * sizeof(float)];
        Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static float[] BytesToFloatArray(byte[] bytes)
    {
        if (bytes == null) return null;
        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }
}
