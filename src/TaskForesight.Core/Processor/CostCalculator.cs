using TaskForesight.Core.Interfaces;

namespace TaskForesight.Core.Processor;

public class CostCalculator : ICostCalculator
{
    public double? CalculateEstimationAccuracy(double? originalEstimateHours, double? timeSpentHours)
    {
        if (originalEstimateHours is null or <= 0 || timeSpentHours is null or <= 0)
            return null;

        return Math.Min(originalEstimateHours.Value, timeSpentHours.Value)
               / Math.Max(originalEstimateHours.Value, timeSpentHours.Value);
    }

    public double? CalculateRealCost(double? timeSpentHours, double? bugFixTimeHours)
    {
        if (timeSpentHours is null && bugFixTimeHours is null)
            return null;

        return (timeSpentHours ?? 0) + (bugFixTimeHours ?? 0);
    }
}
