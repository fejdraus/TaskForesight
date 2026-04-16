namespace TaskForesight.Shared.Dto;

public record DeveloperStats(
    string Name,
    int Tasks,
    double AvgAccuracy,
    double BugRate,
    double AvgCycleTime);

public record DeveloperDetailDto(
    string Name,
    int TotalTasks,
    double AvgAccuracy,
    double BugRate,
    double AvgCycleTime,
    IReadOnlyList<CategoryStats> CategoryBreakdown);
