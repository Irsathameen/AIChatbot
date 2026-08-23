using System.Security.Cryptography;
using System.Text;

namespace AIChatbot.Services
{
    public class SimpleEmbeddingService : IEmbeddingService
    {
        private const int Dimension = 1536;

        public Task<float[]> GetEmbeddingAsync(string text)
        {
            // Deterministic pseudo-embedding: use SHA256 stream to fill floats in [-1,1]
            var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
            using var sha = SHA256.Create();

            var output = new float[Dimension];
            var hash = sha.ComputeHash(bytes);
            // Expand hash as needed
            var buffer = new List<byte>(hash);
            while (buffer.Count < Dimension * 4)
            {
                var more = sha.ComputeHash(buffer.ToArray());
                buffer.AddRange(more);
            }

            for (int i = 0; i < Dimension; i++)
            {
                int idx = i * 4;
                uint v = (uint)(buffer[idx] | (buffer[idx + 1] << 8) | (buffer[idx + 2] << 16) | (buffer[idx + 3] << 24));
                // map to float in [-1,1]
                output[i] = (float)((v / (double)uint.MaxValue) * 2.0 - 1.0);
            }

            return Task.FromResult(output);
        }
    }
}
