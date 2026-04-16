using Hangfire;
using Hangfire.InMemory;
using MudBlazor.Services;
using Serilog;
using TaskForesight.Core.Options;
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
        .WriteTo.Console());

    // Blazor InteractiveAuto
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents()
        .AddInteractiveWebAssemblyComponents();

    // MudBlazor
    builder.Services.AddMudServices();

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

    // Data service (stub — will be replaced in Stage 2+)
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
