namespace TaskForesight.Shared.Dto;

public record TaskSummaryDto(
    string Key,
    string Summary,
    string IssueType,
    string? Assignee,
    double? CycleTime,
    double? RealCostHours,
    int ReturnCount,
    DateTimeOffset? ResolvedAt);

public record TaskDetailDto(
    string Key,
    string Summary,
    string? Description,
    string IssueType,
    string? Assignee,
    string? Reporter,
    IReadOnlyList<string> Components,
    double? CycleTime,
    double? RealCostHours,
    double? EstimationAccuracy,
    int ReturnCount,
    int DirectBugsCount,
    int PostReleaseBugsCount,
    IReadOnlyList<StatusTransitionDto> Transitions,
    IReadOnlyList<TaskLinkDto> Links);

public record StatusTransitionDto(
    string FromStatus,
    string ToStatus,
    string Author,
    DateTimeOffset TransitionedAt,
    double DurationHours);

public record TaskLinkDto(
    string TargetKey,
    string TargetSummary,
    string LinkType,
    string TargetIssueType);

public record TaskFilter(
    string? IssueType,
    string? Assignee,
    string? Category,
    string? Component,
    string? Search,
    int Page = 1,
    int PageSize = 25);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
