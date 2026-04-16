using Microsoft.Extensions.Logging;
using TaskForesight.Core.Interfaces;
using TaskForesight.Shared.Dto;

namespace TaskForesight.Core.Analytics;

public class EstimationService : IEstimationService
{
    private readonly ISimilarityService _similarity;
    private readonly IGraphRepository _graphRepo;
    private readonly ILogger<EstimationService> _logger;

    public EstimationService(ISimilarityService similarity, IGraphRepository graphRepo,
        ILogger<EstimationService> logger)
    {
        _similarity = similarity;
        _graphRepo = graphRepo;
        _logger = logger;
    }

    public async Task<TaskEstimation> EstimateAsync(EstimationRequest request, CancellationToken ct = default)
    {
        var text = $"{request.Summary} {request.Description}".Trim();
        var similarTasks = await _similarity.FindSimilarAsync(text, request.IssueType, 10, ct);

        if (similarTasks.Count == 0)
        {
            return new TaskEstimation(
                BaseEstimateHours: 0,
                PessimisticEstimateHours: 0,
                BugProbability: 0,
                ExpectedReturns: 0,
                AdjustedEstimateHours: 0,
                CrossComponentRisks: [],
                SimilarTasks: [],
                Confidence: 0);
        }

        var costs = similarTasks
            .Where(t => t.RealCostHours > 0)
            .Select(t => t.RealCostHours)
            .OrderBy(x => x)
            .ToList();

        var cycleTimes = similarTasks
            .Where(t => t.CycleTime > 0)
            .Select(t => t.CycleTime)
            .OrderBy(x => x)
            .ToList();

        var baseEstimate = costs.Count > 0 ? Median(costs) : 0;
        var p80Estimate = costs.Count > 0 ? Percentile(costs, 0.8) : 0;

        var avgReturns = similarTasks.Average(t => t.ReturnCount);
        var bugProbability = similarTasks.Count(t => t.ReturnCount > 0) / (double)similarTasks.Count;

        var adjustmentFactor = 1.0 + (bugProbability * 0.3);
        var adjustedEstimate = baseEstimate * adjustmentFactor;

        // Cross-component risks
        var crossRisks = new List<CrossComponentRisk>();
        if (request.Component is not null)
        {
            try
            {
                crossRisks = (await _graphRepo.GetCrossComponentRisksAsync(request.Component, ct)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get cross-component risks for {Component}", request.Component);
            }
        }

        var confidence = Math.Min(1.0, similarTasks.Count / 10.0)
                         * (1.0 - similarTasks.Average(t => t.Distance));

        return new TaskEstimation(
            BaseEstimateHours: baseEstimate,
            PessimisticEstimateHours: p80Estimate,
            BugProbability: bugProbability,
            ExpectedReturns: avgReturns,
            AdjustedEstimateHours: adjustedEstimate,
            CrossComponentRisks: crossRisks,
            SimilarTasks: similarTasks,
            Confidence: confidence);
    }

    private static double Median(List<double> sorted)
    {
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }

    private static double Percentile(List<double> sorted, double p)
    {
        var index = (sorted.Count - 1) * p;
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        if (lower == upper) return sorted[lower];
        return sorted[lower] + (sorted[upper] - sorted[lower]) * (index - lower);
    }
}
