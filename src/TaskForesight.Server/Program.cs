using Hangfire;
using Hangfire.InMemory;
using MudBlazor.Services;
using Npgsql;
using Serilog;
using TaskForesight.Core.Collector;
using TaskForesight.Core.Interfaces;
using TaskForesight.Core.Options;
using TaskForesight.Core.Processor;
using TaskForesight.Core.Storage;
using TaskForesight.Server.Api;
using TaskForesight.Server.Services;
using TaskForesight.Shared.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, config) => config
        .ReadFrom.Configuration(context.Configuration)
        .MinimumLevel.Override("System.Net.Http.HttpClient", Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console());

    // Blazor InteractiveAuto
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents()
        .AddInteractiveWebAssemblyComponents();

    // MudBlazor
    builder.Services.AddMudServices();

    // PostgreSQL (SQL + AGE + pgvector)
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(
        builder.Configuration.GetConnectionString("Analytics"));
    var dataSource = dataSourceBuilder.Build();
    builder.Services.AddSingleton(dataSource);

    // Hangfire
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseInMemoryStorage());
    builder.Services.AddHangfireServer();

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Configuration
    builder.Services.Configure<JiraOptions>(builder.Configuration.GetSection("Jira"));
    builder.Services.Configure<EmbeddingOptions>(builder.Configuration.GetSection("Embedding"));
    builder.Services.Configure<LlmProxyOptions>(builder.Configuration.GetSection("LlmProxy"));
    builder.Services.Configure<AnalyticsOptions>(builder.Configuration.GetSection("Analytics"));

    // Core services
    builder.Services.AddJiraCollector();
    builder.Services.AddStorage();
    builder.Services.AddProcessor();

    // Data service
    builder.Services.AddScoped<IAnalyticsDataService, ServerAnalyticsDataService>();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseWebAssemblyDebugging();
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapStaticAssets();
    app.UseAntiforgery();

    app.MapHangfireDashboard("/hangfire");

    // API
    app.MapCollectionApi();

    // Hangfire recurring jobs
    RecurringJob.AddOrUpdate<IDataProcessor>(
        "incremental-collect",
        x => x.RunIncrementalAsync(CancellationToken.None),
        Cron.Daily(3, 0));

    RecurringJob.AddOrUpdate<ITaskRepository>(
        "refresh-materialized-views",
        x => x.RefreshMaterializedViewsAsync(CancellationToken.None),
        Cron.Daily(4, 0));

    app.MapRazorComponents<TaskForesight.Server.App>()
        .AddInteractiveServerRenderMode()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(TaskForesight.Client._Imports).Assembly);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
