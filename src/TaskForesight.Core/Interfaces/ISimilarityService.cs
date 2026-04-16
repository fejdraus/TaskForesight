using TaskForesight.Shared.Dto;

namespace TaskForesight.Core.Interfaces;

public interface ISimilarityService
{
    Task<IReadOnlyList<SimilarTask>> FindSimilarAsync(string text, string? issueType = null, int limit = 10, CancellationToken ct = default);
}
