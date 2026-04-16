using TaskForesight.Core.Models;

namespace TaskForesight.Core.Interfaces;

public interface IGraphBuilder
{
    Task BuildFromTaskAsync(TaskRecord task, ResolvedLinks links, CancellationToken ct = default);
}
