using TaskForesight.Core.Interfaces;

namespace TaskForesight.Core.Processor;

public class TaskClassifier : ITaskClassifier
{
    public string? Classify(string? issueType, IReadOnlyList<string>? components, IReadOnlyList<string>? labels)
    {
        if (issueType is null)
            return null;

        var component = components?.FirstOrDefault();

        return component is not null
            ? $"{issueType}/{component}"
            : issueType;
    }
}
