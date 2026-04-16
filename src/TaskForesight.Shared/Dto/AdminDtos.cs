namespace TaskForesight.Shared.Dto;

public record CollectionStatus(
    bool IsRunning,
    DateTimeOffset? LastRun,
    int TotalCollected,
    string? CurrentJql);

public record CollectionRequest(
    string? Jql,
    DateTimeOffset? Since,
    bool RebuildGraph,
    bool RebuildEmbeddings);

public record ToxicChain(
    string Key,
    string Summary,
    int TotalBugs,
    double TotalBugFixTime,
    int MaxChainDepth);

public record CrossComponentRisk(
    string ComponentName,
    int CrossBugs);
