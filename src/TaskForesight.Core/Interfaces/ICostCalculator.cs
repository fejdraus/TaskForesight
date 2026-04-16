namespace TaskForesight.Core.Interfaces;

public interface ICostCalculator
{
    double? CalculateEstimationAccuracy(double? originalEstimateHours, double? timeSpentHours);
    double? CalculateRealCost(double? timeSpentHours, double? bugFixTimeHours);
}
