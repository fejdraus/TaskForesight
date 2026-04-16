using Microsoft.Extensions.DependencyInjection;
using TaskForesight.Core.Interfaces;

namespace TaskForesight.Core.Analytics;

public static class AnalyticsServiceExtensions
{
    public static IServiceCollection AddAnalytics(this IServiceCollection services)
    {
        services.AddScoped<IEstimationService, EstimationService>();
        services.AddScoped<ISimilarityService, SimilarityService>();
        return services;
    }
}
