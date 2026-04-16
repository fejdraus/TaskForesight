namespace TaskForesight.Core.Interfaces;

public interface ITaskClassifier
{
    string? Classify(string? issueType, IReadOnlyList<string>? components, IReadOnlyList<string>? labels);
}
