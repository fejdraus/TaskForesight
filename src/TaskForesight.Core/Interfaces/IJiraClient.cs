using TaskForesight.Core.Models;

namespace TaskForesight.Core.Interfaces;

public interface IJiraClient
{
    Task<JiraIssue?> GetIssueAsync(string key, bool expandChangelog = true, CancellationToken ct = default);
    Task<JiraSearchResult> SearchAsync(string jql, int startAt = 0, int maxResults = 50, bool expandChangelog = true, CancellationToken ct = default);
    Task<IReadOnlyList<JiraComment>> GetCommentsAsync(string key, int maxComments = 10, CancellationToken ct = default);
}
