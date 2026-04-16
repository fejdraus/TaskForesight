using Microsoft.Extensions.DependencyInjection;
using TaskForesight.Core.Interfaces;

namespace TaskForesight.Core.Storage;

public static class StorageServiceExtensions
{
    public static IServiceCollection AddStorage(this IServiceCollection services)
    {
        services.AddScoped<ITaskRepository, TaskRepository>();
        return services;
    }
}
