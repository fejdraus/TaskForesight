using TaskForesight.Core.Interfaces;

namespace TaskForesight.Server.Api;

public static class CollectorTestApi
{
    public static void MapCollectorTestApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/test");

        group.MapGet("/collect", async (
            IBatchCollector collector,
            IChangelogParser parser,
            ILinkResolver linkResolver,
            int limit = 5) =>
        {
            var jql = "project = AMCRM ORDER BY updated DESC";
            var results = new List<object>();

            await foreach (var issue in collector.CollectAsync(jql))
            {
                var transitions = parser.ParseTransitions(issue.Changelog);
                var returns = parser.DetectReturns(transitions);
                var links = linkResolver.ResolveLinks(issue);

                results.Add(new
                {
                    issue.Key,
                    IssueType = issue.Fields.IssueType?.Name,
                    Status = issue.Fields.Status?.Name,
                    Summary = issue.Fields.Summary,
                    TransitionCount = transitions.Count,
                    Returns = returns,
                    Transitions = transitions.Select(t => new
                    {
                        t.FromStatus,
                        t.ToStatus,
                        t.Author,
                        t.TransitionedAt
                    }),
                    Links = links.DirectLinks.Select(l => new
                    {
                        l.LinkType,
                        l.Key,
                        l.IssueType,
                        l.Status
                    })
                });

                if (results.Count >= limit) break;
            }

            return Results.Ok(results);
        });
    }
}
