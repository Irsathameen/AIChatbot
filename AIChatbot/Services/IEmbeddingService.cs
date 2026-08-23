namespace AIChatbot.Services
{
    public interface IEmbeddingService
    {
        /// <summary>
        /// Return a deterministic embedding for the input text. Dimension: 1536.
        /// </summary>
        Task<float[]> GetEmbeddingAsync(string text);
    }
}
