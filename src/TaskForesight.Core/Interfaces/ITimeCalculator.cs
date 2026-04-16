using TaskForesight.Core.Models;

namespace TaskForesight.Core.Interfaces;

public interface ITimeCalculator
{
    Dictionary<string, double> CalculateTimeInStatuses(IReadOnlyList<StatusTransition> transitions);
    double? CalculateCycleTime(IReadOnlyList<StatusTransition> transitions);
    double? CalculateLeadTime(DateTimeOffset? createdAt, DateTimeOffset? resolvedAt);
}
