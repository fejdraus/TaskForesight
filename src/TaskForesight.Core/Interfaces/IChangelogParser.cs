using TaskForesight.Core.Models;

namespace TaskForesight.Core.Interfaces;

public interface IChangelogParser
{
    IReadOnlyList<StatusTransition> ParseTransitions(JiraChangelog? changelog);
    int DetectReturns(IReadOnlyList<StatusTransition> transitions);
    string? GetStatusCategory(string statusName);
}
