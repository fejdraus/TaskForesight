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
        ["open"] = ["Open", "Plan", "To Do", "ToDo"],
        ["in_progress"] = ["In Progress", "BUG FIX", "Waiting For Response"],
        ["code_review"] = ["Code Review"],
        ["testing"] = ["Testing", "Completed"],
        ["done"] = ["Done", "Closed", "Released", "Delivery", "Ready for Delivery on Prod", "Canceled"]
    };
}
