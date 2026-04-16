using Microsoft.Extensions.DependencyInjection;
using TaskForesight.Core.Interfaces;

namespace TaskForesight.Core.Processor;

public static class ProcessorServiceExtensions
{
    public static IServiceCollection AddProcessor(this IServiceCollection services)
    {
        services.AddScoped<ITimeCalculator, TimeCalculator>();
        services.AddScoped<ICostCalculator, CostCalculator>();
        services.AddScoped<ITaskClassifier, TaskClassifier>();
        services.AddScoped<IDataProcessor, DataProcessor>();
        return services;
    }
}
