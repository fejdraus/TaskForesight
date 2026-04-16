namespace TaskForesight.Core.Models;

public class TaskRecord
{
    public string Key { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string? IssueType { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public string? Assignee { get; set; }
    public string? Reporter { get; set; }
    public string? ComponentsJson { get; set; }
    public string? LabelsJson { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public double? TimeInOpen { get; set; }
    public double? TimeInProgress { get; set; }
    public double? TimeInCodeReview { get; set; }
    public double? TimeInTesting { get; set; }
    public double? CycleTime { get; set; }
    public double? LeadTime { get; set; }
    public double? OriginalEstimateHours { get; set; }
    public double? TimeSpentHours { get; set; }
    public double? EstimationAccuracy { get; set; }
    public int ReturnCount { get; set; }
    public int ReopenCount { get; set; }
    public int DirectBugsCount { get; set; }
    public int PostReleaseBugsCount { get; set; }
    public double? BugFixTimeHours { get; set; }
    public double? RealCostHours { get; set; }
    public string? TaskCategory { get; set; }
}
