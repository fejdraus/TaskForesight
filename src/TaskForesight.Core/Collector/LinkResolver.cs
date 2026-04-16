using System.Globalization;
using Microsoft.Extensions.Logging;
using TaskForesight.Core.Interfaces;
using TaskForesight.Core.Models;

namespace TaskForesight.Core.Collector;

public class LinkResolver : ILinkResolver
{
    private readonly IJiraClient _jiraClient;
    private readonly ILogger<LinkResolver> _logger;

    public LinkResolver(IJiraClient jiraClient, ILogger<LinkResolver> logger)
    {
        _jiraClient = jiraClient;
        _logger = logger;
    }

    public ResolvedLinks ResolveLinks(JiraIssue issue)
    {
        var directLinks = new List<LinkedIssueInfo>();

        foreach (var link in issue.Fields.IssueLinks)
        {
            if (link.OutwardIssue is { } outward)
            {
                directLinks.Add(new LinkedIssueInfo(
                    Key: outward.Key,
                    Summary: outward.Fields.Summary ?? string.Empty,
                    LinkType: link.Type.Outward,
                    IssueType: outward.Fields.IssueType?.Name ?? string.Empty,
                    Status: outward.Fields.Status?.Name ?? string.Empty));
            }
            else if (link.InwardIssue is { } inward)
            {
                directLinks.Add(new LinkedIssueInfo(
                    Key: inward.Key,
                    Summary: inward.Fields.Summary ?? string.Empty,
                    LinkType: link.Type.Inward,
                    IssueType: inward.Fields.IssueType?.Name ?? string.Empty,
                    Status: inward.Fields.Status?.Name ?? string.Empty));
            }
        }

        return new ResolvedLinks(DirectLinks: directLinks, PostReleaseBugs: []);
    }

    public async Task<IReadOnlyList<JiraIssue>> FindPostReleaseBugsAsync(string key,
        DateTimeOffset? resolvedAt, CancellationToken ct = default)
    {
        if (resolvedAt is null)
            return [];

        var resolvedDate = resolvedAt.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var jql = $"issue in linkedIssues({key}) AND issuetype = Bug AND created > \"{resolvedDate}\"";

        _logger.LogDebug("Searching post-release bugs for {Key}: {Jql}", key, jql);

        try
        {
            var result = await _jiraClient.SearchAsync(jql, startAt: 0, maxResults: 50,
                expandChangelog: false, ct);
            return result.Issues;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to find post-release bugs for {Key}", key);
            return [];
        }
    }
}
