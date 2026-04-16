using TaskForesight.Shared.Dto;

namespace TaskForesight.Shared.Services;

public interface IAnalyticsDataService
{
    // Dashboard
    Task<DashboardStats> GetDashboardStatsAsync();
    Task<IReadOnlyList<CategoryStats>> GetCategoryStatsAsync();
    Task<IReadOnlyList<ToxicChain>> GetToxicChainsAsync(int limit = 10);

    // Estimation
    Task<TaskEstimation> EstimateAsync(EstimationRequest request);
    Task<IReadOnlyList<SimilarTask>> FindSimilarAsync(string text, string? issueType = null, int limit = 10);

    // Tasks
    Task<PagedResult<TaskSummaryDto>> GetTasksAsync(TaskFilter filter);
    Task<TaskDetailDto?> GetTaskDetailAsync(string key);

    // Team
    Task<IReadOnlyList<DeveloperStats>> GetDeveloperStatsAsync();
    Task<DeveloperDetailDto?> GetDeveloperDetailAsync(string name);

    // Graph
    Task<GraphData> GetTaskGraphAsync(string key, int depth = 3);
    Task<IReadOnlyList<CrossComponentRisk>> GetCrossComponentRisksAsync(string component);

    // Admin
    Task<CollectionStatus> GetCollectionStatusAsync();
    Task StartCollectionAsync(CollectionRequest request);

    // Autocomplete
    Task<IReadOnlyList<string>> GetDevelopersAsync();
    Task<IReadOnlyList<string>> GetComponentsAsync();
}
