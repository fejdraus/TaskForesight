namespace TaskForesight.Shared.Dto;

public record EstimationRequest(
    string Summary,
    string? Description,
    string IssueType,
    string? Assignee,
    string? Component);

public record TaskEstimation(
    double BaseEstimateHours,
    double PessimisticEstimateHours,
    double BugProbability,
    double ExpectedReturns,
    double AdjustedEstimateHours,
    IReadOnlyList<CrossComponentRisk> CrossComponentRisks,
    IReadOnlyList<SimilarTask> SimilarTasks,
    double Confidence);

public record SimilarTask(
    string Key,
    string Summary,
    double RealCostHours,
    int ReturnCount,
    double CycleTime,
    double Distance);
