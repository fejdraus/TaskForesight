using TaskForesight.Core.Models;

namespace TaskForesight.Core.Interfaces;

public interface ILinkResolver
{
    ResolvedLinks ResolveLinks(JiraIssue issue);
    Task<IReadOnlyList<JiraIssue>> FindPostReleaseBugsAsync(string key, DateTimeOffset? resolvedAt, CancellationToken ct = default);
}
