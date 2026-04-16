using TaskForesight.Core.Models;

namespace TaskForesight.Core.Interfaces;

public interface IBatchCollector
{
    IAsyncEnumerable<JiraIssue> CollectAsync(string jql, CancellationToken ct = default);
    Task<int> CollectAndSaveRawAsync(string jql, string outputDir, CancellationToken ct = default);
}
