using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using Pgvector;
using TaskForesight.Core.Interfaces;
using TaskForesight.Shared.Dto;

namespace TaskForesight.Core.Storage;

public class VectorRepository : IVectorRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<VectorRepository> _logger;

    public VectorRepository(NpgsqlDataSource dataSource, ILogger<VectorRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task UpdateEmbeddingAsync(string key, float[] embedding, CancellationToken ct = default)
    {
        const string sql = "UPDATE tasks SET embedding = @Embedding::vector WHERE key = @Key";
        var vector = new Vector(embedding);

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { Key = key, Embedding = vector });
        _logger.LogDebug("Updated embedding for {Key}", key);
    }

    public async Task<IReadOnlyList<SimilarTask>> FindSimilarAsync(float[] queryEmbedding, int limit = 10,
        string? issueType = null, CancellationToken ct = default)
    {
        var vector = new Vector(queryEmbedding);
        var typeFilter = issueType is not null ? "AND issue_type = @IssueType" : "";

        var sql = $"""
            SELECT key, summary, real_cost_hours AS realcosthours, return_count AS returncount,
                   cycle_time AS cycletime, embedding <=> @Query::vector AS distance
            FROM tasks
            WHERE embedding IS NOT NULL {typeFilter}
            ORDER BY embedding <=> @Query::vector
            LIMIT @Limit
            """;

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var results = await conn.QueryAsync<SimilarTask>(sql, new
        {
            Query = vector,
            Limit = limit,
            IssueType = issueType
        });

        return results.ToList();
    }
}
