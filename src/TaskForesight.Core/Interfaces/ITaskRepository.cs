using TaskForesight.Core.Models;

namespace TaskForesight.Core.Interfaces;

public interface ITaskRepository
{
    Task UpsertTaskAsync(TaskRecord task, CancellationToken ct = default);
    Task UpsertTransitionsAsync(string taskKey, IReadOnlyList<StatusTransition> transitions, CancellationToken ct = default);
    Task<DateTimeOffset?> GetLastCollectedAtAsync(CancellationToken ct = default);
    Task RefreshMaterializedViewsAsync(CancellationToken ct = default);
}
