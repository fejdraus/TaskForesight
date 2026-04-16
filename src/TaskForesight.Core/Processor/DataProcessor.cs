using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskForesight.Core.Interfaces;
using TaskForesight.Core.Models;

namespace TaskForesight.Core.Processor;

public class DataProcessor : IDataProcessor
{
    private readonly IBatchCollector _collector;
    private readonly IChangelogParser _parser;
    private readonly ILinkResolver _linkResolver;
    private readonly ITimeCalculator _timeCalculator;
    private readonly ICostCalculator _costCalculator;
    private readonly ITaskClassifier _classifier;
    private readonly ITaskRepository _repository;
    private readonly ILogger<DataProcessor> _logger;

    public DataProcessor(
        IBatchCollector collector,
        IChangelogParser parser,
        ILinkResolver linkResolver,
        ITimeCalculator timeCalculator,
        ICostCalculator costCalculator,
        ITaskClassifier classifier,
        ITaskRepository repository,
        ILogger<DataProcessor> logger)
    {
        _collector = collector;
        _parser = parser;
        _linkResolver = linkResolver;
        _timeCalculator = timeCalculator;
        _costCalculator = costCalculator;
        _classifier = classifier;
        _repository = repository;
        _logger = logger;
    }

    public async Task RunFullCollectionAsync(string jql, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting full collection: {Jql}", jql);
        int count = 0;

        await foreach (var issue in _collector.CollectAsync(jql, ct))
        {
            await ProcessAndStoreAsync(issue, ct);
            count++;
        }

        await _repository.RefreshMaterializedViewsAsync(ct);
        _logger.LogInformation("Full collection complete: {Count} issues processed", count);
    }

    public async Task RunIncrementalAsync(CancellationToken ct = default)
    {
        var lastCollected = await _repository.GetLastCollectedAtAsync(ct);
        var since = lastCollected?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                    ?? DateTime.UtcNow.AddMonths(-6).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var jql = $"updated >= \"{since}\" ORDER BY updated ASC";

        _logger.LogInformation("Starting incremental collection since {Since}", since);
        int count = 0;

        await foreach (var issue in _collector.CollectAsync(jql, ct))
        {
            await ProcessAndStoreAsync(issue, ct);
            count++;
        }

        if (count > 0)
            await _repository.RefreshMaterializedViewsAsync(ct);

        _logger.LogInformation("Incremental collection complete: {Count} issues processed", count);
    }

    private async Task ProcessAndStoreAsync(JiraIssue issue, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(issue.Fields.Description))
        {
            _logger.LogDebug("Skipping {Key} — no description", issue.Key);
            return;
        }

        var transitions = _parser.ParseTransitions(issue.Changelog);
        var returns = _parser.DetectReturns(transitions);
        var links = _linkResolver.ResolveLinks(issue);
        var timeInStatuses = _timeCalculator.CalculateTimeInStatuses(transitions);

        var createdAt = ParseDate(issue.Fields.Created);
        var resolvedAt = ParseDate(issue.Fields.ResolutionDate);

        var originalEstimateSeconds = issue.Fields.AggregateTimeOriginalEstimate
                                     ?? issue.Fields.TimeOriginalEstimate;
        var timeSpentSeconds = issue.Fields.AggregateTimeSpent
                               ?? issue.Fields.TimeSpent;

        var originalEstimateHours = originalEstimateSeconds.HasValue
            ? originalEstimateSeconds.Value / 3600.0
            : (double?)null;

        var timeSpentHours = timeSpentSeconds.HasValue
            ? timeSpentSeconds.Value / 3600.0
            : (double?)null;

        var components = issue.Fields.Components.Select(c => c.Name).ToList();
        var labels = issue.Fields.Labels;

        var record = new TaskRecord
        {
            Key = issue.Key,
            Summary = issue.Fields.Summary,
            Description = issue.Fields.Description,
            IssueType = issue.Fields.IssueType?.Name,
            Priority = issue.Fields.Priority?.Name,
            Status = issue.Fields.Status?.Name,
            Assignee = issue.Fields.Assignee?.Name,
            Reporter = issue.Fields.Reporter?.Name,
            ComponentsJson = JsonSerializer.Serialize(components),
            LabelsJson = JsonSerializer.Serialize(labels),
            CreatedAt = createdAt,
            ResolvedAt = resolvedAt,
            TimeInOpen = timeInStatuses.GetValueOrDefault("open"),
            TimeInProgress = timeInStatuses.GetValueOrDefault("in_progress"),
            TimeInCodeReview = timeInStatuses.GetValueOrDefault("code_review"),
            TimeInTesting = timeInStatuses.GetValueOrDefault("testing"),
            CycleTime = _timeCalculator.CalculateCycleTime(transitions),
            LeadTime = _timeCalculator.CalculateLeadTime(createdAt, resolvedAt),
            OriginalEstimateHours = originalEstimateHours,
            TimeSpentHours = timeSpentHours,
            EstimationAccuracy = _costCalculator.CalculateEstimationAccuracy(originalEstimateHours, timeSpentHours),
            ReturnCount = returns,
            DirectBugsCount = links.DirectLinks.Count(l =>
                l.IssueType.Equals("Bug", StringComparison.OrdinalIgnoreCase)),
            PostReleaseBugsCount = links.PostReleaseBugs.Count,
            RealCostHours = _costCalculator.CalculateRealCost(timeSpentHours, null),
            TaskCategory = _classifier.Classify(issue.Fields.IssueType?.Name, components, labels)
        };

        await _repository.UpsertTaskAsync(record, ct);
        await _repository.UpsertTransitionsAsync(issue.Key, transitions, ct);
    }

    private static DateTimeOffset? ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr))
            return null;

        return DateTimeOffset.TryParse(dateStr, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var result)
            ? result.ToUniversalTime()
            : null;
    }
}
