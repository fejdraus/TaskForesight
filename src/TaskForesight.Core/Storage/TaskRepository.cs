using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using TaskForesight.Core.Interfaces;
using TaskForesight.Core.Models;

namespace TaskForesight.Core.Storage;

public class TaskRepository : ITaskRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<TaskRepository> _logger;

    public TaskRepository(NpgsqlDataSource dataSource, ILogger<TaskRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task UpsertTaskAsync(TaskRecord task, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO tasks (
                key, summary, description, issue_type, priority, status,
                assignee, reporter, components, labels,
                created_at, resolved_at,
                time_in_open, time_in_progress, time_in_code_review, time_in_testing,
                cycle_time, lead_time,
                original_estimate_hours, time_spent_hours, estimation_accuracy,
                return_count, reopen_count,
                direct_bugs_count, post_release_bugs_count, bug_fix_time_hours,
                real_cost_hours, task_category, collected_at
            ) VALUES (
                @Key, @Summary, @Description, @IssueType, @Priority, @Status,
                @Assignee, @Reporter, @ComponentsJson::jsonb, @LabelsJson::jsonb,
                @CreatedAt, @ResolvedAt,
                @TimeInOpen, @TimeInProgress, @TimeInCodeReview, @TimeInTesting,
                @CycleTime, @LeadTime,
                @OriginalEstimateHours, @TimeSpentHours, @EstimationAccuracy,
                @ReturnCount, @ReopenCount,
                @DirectBugsCount, @PostReleaseBugsCount, @BugFixTimeHours,
                @RealCostHours, @TaskCategory, NOW()
            )
            ON CONFLICT (key) DO UPDATE SET
                summary = EXCLUDED.summary,
                description = EXCLUDED.description,
                issue_type = EXCLUDED.issue_type,
                priority = EXCLUDED.priority,
                status = EXCLUDED.status,
                assignee = EXCLUDED.assignee,
                reporter = EXCLUDED.reporter,
                components = EXCLUDED.components,
                labels = EXCLUDED.labels,
                created_at = EXCLUDED.created_at,
                resolved_at = EXCLUDED.resolved_at,
                time_in_open = EXCLUDED.time_in_open,
                time_in_progress = EXCLUDED.time_in_progress,
                time_in_code_review = EXCLUDED.time_in_code_review,
                time_in_testing = EXCLUDED.time_in_testing,
                cycle_time = EXCLUDED.cycle_time,
                lead_time = EXCLUDED.lead_time,
                original_estimate_hours = EXCLUDED.original_estimate_hours,
                time_spent_hours = EXCLUDED.time_spent_hours,
                estimation_accuracy = EXCLUDED.estimation_accuracy,
                return_count = EXCLUDED.return_count,
                reopen_count = EXCLUDED.reopen_count,
                direct_bugs_count = EXCLUDED.direct_bugs_count,
                post_release_bugs_count = EXCLUDED.post_release_bugs_count,
                bug_fix_time_hours = EXCLUDED.bug_fix_time_hours,
                real_cost_hours = EXCLUDED.real_cost_hours,
                task_category = EXCLUDED.task_category,
                collected_at = NOW()
            """;

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(sql, task);
        _logger.LogDebug("Upserted task {Key}", task.Key);
    }

    public async Task UpsertTransitionsAsync(string taskKey, IReadOnlyList<StatusTransition> transitions,
        CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        await conn.ExecuteAsync(
            "DELETE FROM status_transitions WHERE task_key = @TaskKey",
            new { TaskKey = taskKey }, tx);

        if (transitions.Count > 0)
        {
            const string sql = """
                INSERT INTO status_transitions (task_key, from_status, to_status, author, transitioned_at, duration_hours)
                VALUES (@TaskKey, @FromStatus, @ToStatus, @Author, @TransitionedAt, @DurationHours)
                """;

            for (int i = 0; i < transitions.Count; i++)
            {
                var t = transitions[i];
                double? durationHours = null;
                if (i + 1 < transitions.Count)
                    durationHours = (transitions[i + 1].TransitionedAt - t.TransitionedAt).TotalHours;

                await conn.ExecuteAsync(sql, new
                {
                    TaskKey = taskKey,
                    t.FromStatus,
                    t.ToStatus,
                    t.Author,
                    t.TransitionedAt,
                    DurationHours = durationHours
                }, tx);
            }
        }

        await tx.CommitAsync(ct);
        _logger.LogDebug("Upserted {Count} transitions for {Key}", transitions.Count, taskKey);
    }

    public async Task<DateTimeOffset?> GetLastCollectedAtAsync(CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<DateTimeOffset?>(
            "SELECT MAX(collected_at) FROM tasks");
    }

    public async Task RefreshMaterializedViewsAsync(CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await conn.ExecuteAsync("REFRESH MATERIALIZED VIEW category_stats");
        _logger.LogInformation("Refreshed materialized views");
    }
}
