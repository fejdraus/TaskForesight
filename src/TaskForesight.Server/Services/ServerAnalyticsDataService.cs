using TaskForesight.Shared.Dto;
using TaskForesight.Shared.Services;

namespace TaskForesight.Server.Services;

public class ServerAnalyticsDataService : IAnalyticsDataService
{
    public Task<DashboardStats> GetDashboardStatsAsync()
        => throw new NotImplementedException();

    public Task<IReadOnlyList<CategoryStats>> GetCategoryStatsAsync()
        => throw new NotImplementedException();

    public Task<IReadOnlyList<ToxicChain>> GetToxicChainsAsync(int limit = 10)
        => throw new NotImplementedException();

    public Task<TaskEstimation> EstimateAsync(EstimationRequest request)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<SimilarTask>> FindSimilarAsync(string text, string? issueType = null, int limit = 10)
        => throw new NotImplementedException();

    public Task<PagedResult<TaskSummaryDto>> GetTasksAsync(TaskFilter filter)
        => throw new NotImplementedException();

    public Task<TaskDetailDto?> GetTaskDetailAsync(string key)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<DeveloperStats>> GetDeveloperStatsAsync()
        => throw new NotImplementedException();

    public Task<DeveloperDetailDto?> GetDeveloperDetailAsync(string name)
        => throw new NotImplementedException();

    public Task<GraphData> GetTaskGraphAsync(string key, int depth = 3)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<CrossComponentRisk>> GetCrossComponentRisksAsync(string component)
        => throw new NotImplementedException();

    public Task<CollectionStatus> GetCollectionStatusAsync()
        => throw new NotImplementedException();

    public Task StartCollectionAsync(CollectionRequest request)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<string>> GetDevelopersAsync()
        => throw new NotImplementedException();

    public Task<IReadOnlyList<string>> GetComponentsAsync()
        => throw new NotImplementedException();
}
