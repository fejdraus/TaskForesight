namespace TaskForesight.Core.Options;

public class AnalyticsOptions
{
    public int DefaultHistoryMonths { get; set; } = 6;
    public int SimilarTasksLimit { get; set; } = 10;
}
