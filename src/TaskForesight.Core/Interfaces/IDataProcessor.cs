namespace TaskForesight.Core.Interfaces;

public interface IDataProcessor
{
    Task RunFullCollectionAsync(string jql, CancellationToken ct = default);
    Task RunIncrementalAsync(CancellationToken ct = default);
}
