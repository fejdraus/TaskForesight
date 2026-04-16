using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskForesight.Core.Interfaces;
using TaskForesight.Core.Models;

namespace TaskForesight.Core.Collector;

public class BatchCollector : IBatchCollector
{
    private readonly IJiraClient _jiraClient;
    private readonly ILogger<BatchCollector> _logger;
    private const int PageSize = 50;

    public BatchCollector(IJiraClient jiraClient, ILogger<BatchCollector> logger)
    {
        _jiraClient = jiraClient;
        _logger = logger;
    }

    public async IAsyncEnumerable<JiraIssue> CollectAsync(string jql,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        int startAt = 0;
        int total;
        int collected = 0;

        do
        {
            var result = await _jiraClient.SearchAsync(jql, startAt, PageSize, expandChangelog: true, ct);
            total = result.Total;

            if (result.Issues.Count == 0)
                break;

            foreach (var issue in result.Issues)
            {
                collected++;
                yield return issue;
            }

            if (collected % 50 == 0 || collected == total)
                _logger.LogInformation("Collected {Collected}/{Total} issues", collected, total);

            startAt += result.Issues.Count;

        } while (startAt < total && !ct.IsCancellationRequested);

        _logger.LogInformation("Collection complete: {Collected} issues from JQL: {Jql}", collected, jql);
    }

    public async Task<int> CollectAndSaveRawAsync(string jql, string outputDir, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDir);

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        int count = 0;

        await foreach (var issue in CollectAsync(jql, ct))
        {
            var filePath = Path.Combine(outputDir, $"{issue.Key}.json");
            var json = JsonSerializer.Serialize(issue, jsonOptions);
            await File.WriteAllTextAsync(filePath, json, ct);
            count++;
        }

        _logger.LogInformation("Saved {Count} raw JSON files to {Dir}", count, outputDir);
        return count;
    }
}
