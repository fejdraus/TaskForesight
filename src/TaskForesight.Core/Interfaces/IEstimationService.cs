using TaskForesight.Shared.Dto;

namespace TaskForesight.Core.Interfaces;

public interface IEstimationService
{
    Task<TaskEstimation> EstimateAsync(EstimationRequest request, CancellationToken ct = default);
}
