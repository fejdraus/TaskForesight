using TaskForesight.Core.Interfaces;
using TaskForesight.Core.Models;

namespace TaskForesight.Core.Processor;

public class TimeCalculator : ITimeCalculator
{
    private readonly IChangelogParser _parser;

    public TimeCalculator(IChangelogParser parser)
    {
        _parser = parser;
    }

    public Dictionary<string, double> CalculateTimeInStatuses(IReadOnlyList<StatusTransition> transitions)
    {
        var result = new Dictionary<string, double>
        {
            ["open"] = 0,
            ["in_progress"] = 0,
            ["code_review"] = 0,
            ["testing"] = 0,
            ["done"] = 0
        };

        for (int i = 0; i < transitions.Count; i++)
        {
            var category = _parser.GetStatusCategory(transitions[i].ToStatus);
            if (category is null) continue;

            DateTimeOffset end = i + 1 < transitions.Count
                ? transitions[i + 1].TransitionedAt
                : DateTimeOffset.UtcNow;

            var hours = (end - transitions[i].TransitionedAt).TotalHours;
            if (hours > 0)
                result[category] += hours;
        }

        return result;
    }

    public double? CalculateCycleTime(IReadOnlyList<StatusTransition> transitions)
    {
        DateTimeOffset? firstInProgress = null;
        DateTimeOffset? lastDone = null;

        foreach (var t in transitions)
        {
            var toCategory = _parser.GetStatusCategory(t.ToStatus);

            if (toCategory == "in_progress" && firstInProgress is null)
                firstInProgress = t.TransitionedAt;

            if (toCategory == "done")
                lastDone = t.TransitionedAt;
        }

        if (firstInProgress is null || lastDone is null)
            return null;

        var hours = (lastDone.Value - firstInProgress.Value).TotalHours;
        return hours > 0 ? hours : null;
    }

    public double? CalculateLeadTime(DateTimeOffset? createdAt, DateTimeOffset? resolvedAt)
    {
        if (createdAt is null || resolvedAt is null)
            return null;

        var hours = (resolvedAt.Value - createdAt.Value).TotalHours;
        return hours > 0 ? hours : null;
    }
}
