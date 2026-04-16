using TaskForesight.Core.Interfaces;
using TaskForesight.Shared.Dto;

namespace TaskForesight.Server.Api;

public static class CollectionApiExtensions
{
    private static bool _isRunning;
    private static string? _currentJql;
    private static int _collected;
    private static DateTimeOffset? _lastRun;

    public static void MapCollectionApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/collection");

        group.MapPost("/start", (CollectionRequest request, IServiceProvider sp, ILogger<Program> logger) =>
        {
            if (_isRunning)
                return Results.Conflict(new { message = "Collection is already running" });

            const string defaultJql = "project = AMCRM AND resolution in (Done, Fixed) AND description is not EMPTY ORDER BY updated DESC";
            var jql = string.IsNullOrWhiteSpace(request.Jql) || request.Jql == "string"
                ? defaultJql
                : request.Jql;

            _isRunning = true;
            _currentJql = jql;
            _collected = 0;

            _ = Task.Run(async () =>
            {
                using var scope = sp.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IDataProcessor>();
                try
                {
                    await processor.RunFullCollectionAsync(jql);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Collection failed for JQL: {Jql}", jql);
                }
                finally
                {
                    _isRunning = false;
                    _lastRun = DateTimeOffset.UtcNow;
                }
            });

            return Results.Accepted(value: new { message = "Collection started", jql });
        });

        group.MapGet("/status", () =>
        {
            return Results.Ok(new CollectionStatus(
                IsRunning: _isRunning,
                LastRun: _lastRun,
                TotalCollected: _collected,
                CurrentJql: _currentJql));
        });
    }
}
