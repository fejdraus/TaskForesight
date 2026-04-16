namespace TaskForesight.Core.Models;

public record ResolvedLinks(
    IReadOnlyList<LinkedIssueInfo> DirectLinks,
    IReadOnlyList<LinkedIssueInfo> PostReleaseBugs);

public record LinkedIssueInfo(
    string Key,
    string Summary,
    string LinkType,
    string IssueType,
    string Status);
