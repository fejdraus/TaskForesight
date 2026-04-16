using TaskForesight.Shared.Dto;

namespace TaskForesight.Core.Interfaces;

public interface IVectorRepository
{
    Task UpdateEmbeddingAsync(string key, float[] embedding, CancellationToken ct = default);
    Task<IReadOnlyList<SimilarTask>> FindSimilarAsync(float[] queryEmbedding, int limit = 10, string? issueType = null, CancellationToken ct = default);
}
