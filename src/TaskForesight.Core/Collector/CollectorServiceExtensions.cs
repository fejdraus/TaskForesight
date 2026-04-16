using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaskForesight.Core.Interfaces;
using TaskForesight.Core.Options;

namespace TaskForesight.Core.Collector;

public static class CollectorServiceExtensions
{
    public static IServiceCollection AddJiraCollector(this IServiceCollection services)
    {
        services.AddHttpClient<IJiraClient, JiraClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<JiraOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            var auth = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{options.Username}:{options.Credential}"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", auth);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = options.RequestTimeout;
        });

        services.AddScoped<IBatchCollector, BatchCollector>();
        services.AddScoped<IChangelogParser, ChangelogParser>();
        services.AddScoped<ILinkResolver, LinkResolver>();

        return services;
    }
}
