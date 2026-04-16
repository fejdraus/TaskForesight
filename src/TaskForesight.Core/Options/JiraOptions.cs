namespace TaskForesight.Core.Options;

public class JiraOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Credential { get; set; } = string.Empty;
    public int MaxComments { get; set; } = 10;
    public bool IncludeLinked { get; set; } = true;
    public int MaxConcurrentRequests { get; set; } = 4;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(15);

    public Dictionary<string, string[]> StatusMapping { get; set; } = new()
    {
        ["open"] = ["Open", "New", "To Do", "Backlog", "Reopened", "Plan"],
        ["in_progress"] = ["In Progress", "In Development", "Coding", "BUG FIX"],
        ["code_review"] = ["Code Review", "Review", "In Review", "PR Review"],
        ["testing"] = ["Testing", "QA", "In QA", "Verification", "In Testing"],
        ["done"] = ["Done", "Closed", "Resolved", "Released", "Delivery", "Ready for Delivery on Prod"]
    };
}
