using System.Globalization;
using Microsoft.Extensions.Options;
using TaskForesight.Core.Interfaces;
using TaskForesight.Core.Models;
using TaskForesight.Core.Options;

namespace TaskForesight.Core.Collector;

public class ChangelogParser : IChangelogParser
{
    private readonly Dictionary<string, string> _statusToCategory;
    private static readonly string[] CategoryOrder = ["open", "in_progress", "code_review", "testing", "done"];

    public ChangelogParser(IOptions<JiraOptions> options)
    {
        _statusToCategory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (category, statuses) in options.Value.StatusMapping)
        {
            foreach (var status in statuses)
                _statusToCategory[status] = category;
        }
    }

    public IReadOnlyList<StatusTransition> ParseTransitions(JiraChangelog? changelog)
    {
        if (changelog is null)
            return [];

        var transitions = new List<StatusTransition>();

        foreach (var history in changelog.Histories.OrderBy(h => h.Created))
        {
            foreach (var item in history.Items)
            {
                if (!string.Equals(item.Field, "status", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (DateTimeOffset.TryParse(history.Created, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var timestamp))
                {
                    transitions.Add(new StatusTransition(
                        FromStatus: item.FromString ?? string.Empty,
                        ToStatus: item.ToString ?? string.Empty,
                        Author: history.Author.Name,
                        TransitionedAt: timestamp));
                }
            }
        }

        return transitions;
    }

    public int DetectReturns(IReadOnlyList<StatusTransition> transitions)
    {
        int returns = 0;

        foreach (var t in transitions)
        {
            var fromCategory = GetStatusCategory(t.FromStatus);
            var toCategory = GetStatusCategory(t.ToStatus);

            if (fromCategory is null || toCategory is null)
                continue;

            var fromIndex = Array.IndexOf(CategoryOrder, fromCategory);
            var toIndex = Array.IndexOf(CategoryOrder, toCategory);

            if (fromIndex > toIndex && fromIndex >= 0 && toIndex >= 0)
                returns++;
        }

        return returns;
    }

    public string? GetStatusCategory(string statusName)
    {
        return _statusToCategory.TryGetValue(statusName, out var category) ? category : null;
    }
}
