using TaskForesight.Shared.Dto;

namespace TaskForesight.Core.Interfaces;

public interface IGraphRepository
{
    Task<GraphData> GetTaskGraphAsync(string key, int depth = 3, CancellationToken ct = default);
    Task<IReadOnlyList<CrossComponentRisk>> GetCrossComponentRisksAsync(string component, CancellationToken ct = default);
}
