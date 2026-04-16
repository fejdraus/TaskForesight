namespace TaskForesight.Shared.Dto;

public record DashboardStats(
    double AvgCycleTimeDays,
    double AvgEstimationAccuracy,
    double AvgReturnRate,
    double AvgPostReleaseBugs,
    int TotalTasks,
    int TotalBugs,
    DateTimeOffset LastCollectedAt);

public record CategoryStats(
    string Category,
    int SampleCount,
    double AvgCycleTime,
    double MedianCycleTime,
    double AvgEstimationAccuracy,
    double AvgReturnRate,
    double AvgPostReleaseBugs,
    double MedianRealCost,
    double P80RealCost);
