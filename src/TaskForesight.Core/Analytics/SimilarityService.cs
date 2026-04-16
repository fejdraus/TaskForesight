using Microsoft.Extensions.Logging;
using TaskForesight.Core.Interfaces;
using TaskForesight.Shared.Dto;

namespace TaskForesight.Core.Analytics;

public class SimilarityService : ISimilarityService
{
    private readonly IEmbeddingGenerator _embeddings;
    private readonly IVectorRepository _vectorRepo;
    private readonly ILogger<SimilarityService> _logger;

    public SimilarityService(IEmbeddingGenerator embeddings, IVectorRepository vectorRepo,
        ILogger<SimilarityService> logger)
    {
        _embeddings = embeddings;
        _vectorRepo = vectorRepo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SimilarTask>> FindSimilarAsync(string text, string? issueType = null,
        int limit = 10, CancellationToken ct = default)
    {
        var embedding = await _embeddings.GenerateAsync(text, ct);
        if (embedding is null)
        {
            _logger.LogWarning("Failed to generate embedding for similarity search");
            return [];
        }

        return await _vectorRepo.FindSimilarAsync(embedding, limit, issueType, ct);
    }
}
