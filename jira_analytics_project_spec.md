# Jira Analytics Knowledge Base

## Название проекта
**TaskForesight** — система предиктивной оценки задач на основе исторических данных Jira

---

## Цель проекта

**Самостоятельный проект** (не часть ai-review). Построить базу знаний из исторических данных Jira для предиктивной аналитики задач.

### Три точки доступа к данным:

1. **Веб-интерфейс (Blazor)** — для менеджеров и тимлидов:
   - Дашборд с ключевыми метриками команды
   - Интерактивная оценка задач с AI-подсказками
   - Визуализация графа зависимостей
   - Отчёты по спринтам, разработчикам, компонентам

2. **MCP Server** — для AI-агентов (Claude Code и др.):
   - AI запрашивает аналитику через MCP протокол
   - Получает оценки, риски, похожие задачи в markdown
   - Использует данные для своих ответов пользователю

3. **REST API** — для любых внешних систем:
   - Telegram-боты, CI/CD pipelines, дашборды, скрипты
   - Будущие интеграции (ai-review, Jira webhooks и т.д.)

### Что система умеет:
- Давать реалистичные оценки времени для новых задач
- Выявлять паттерны (какие типы задач занижаются в оценке)
- Учитывать скрытую стоимость (баги после закрытия, связанные доработки)
- Предсказывать вероятность возврата из тестирования
- Рекомендовать коэффициенты корректировки оценок
- Находить причинно-следственные связи через граф задач
- Искать семантически похожие задачи по описанию

---

## Проблема которую решаем

Сейчас оценки задач делаются "на глаз":
- Не учитываются баги, которые появятся при тестировании
- Не учитываются баги после закрытия задачи (production bugs)
- Не видно, что интеграции стабильно занижаются в оценке в 2-3 раза
- Нет данных сколько времени задача реально провела в каждом статусе
- Связанные задачи (sub-tasks, caused by, relates to) не учитываются в стоимости
- Нет поиска по смыслу — нельзя найти "похожие задачи" по описанию
- Не видны цепочки зависимостей и паттерны "заражения" между компонентами

---

## Технологический стек

### Ядро: PostgreSQL Multi-Model (Graph + Vector + SQL)

Одна база данных, три модели данных:

| Модель | Расширение | Что решает |
|---|---|---|
| **Граф** | Apache AGE | Связи между задачами, цепочки багов, зависимости разработчиков и компонентов |
| **Вектор** | pgvector | Семантический поиск похожих задач по описанию (embeddings) |
| **SQL** | встроенный | Точная аналитика, агрегации, статистика, отчёты |

### Полный стек

- **Фреймворк:** .NET 10+
- **UI:** Blazor InteractiveAuto (Server + WebAssembly гибрид) + MudBlazor
- **БД:** PostgreSQL 16+ с расширениями Apache AGE + pgvector
- **ORM/клиент:** Npgsql + Dapper
- **Apache AGE клиент:** Konnektr.Npgsql.Age — нативная интеграция с Npgsql через `UseAge()`
- **Векторный поиск:** Pgvector NuGet — маппинг vector типов для Npgsql через `UseVector()`
- **HTTP клиент:** HttpClient + IHttpClientFactory (для Jira REST API)
- **LLM прокси:** CLIProxyAPIPlus (https://github.com/router-for-me/CLIProxyAPIPlus) — OpenAI-совместимый прокси для chat completions. Единая точка входа к Gemini, Claude, GPT, Grok. Используется для AI-анализа текста задач (классификация, извлечение features)
- **Embeddings:** Напрямую через API провайдера (CLIProxyAPIPlus НЕ поддерживает /v1/embeddings). Варианты:
  - OpenAI API напрямую (text-embedding-3-small, ~$0.02/1M токенов)
  - Grok embedding API напрямую
  - Локальная модель (e.g. sentence-transformers через Python sidecar или Ollama)
  - Gemini Embedding API напрямую (бесплатно, models/text-embedding-004)
- **Фоновые задачи:** Hangfire (InMemory или PostgreSQL storage)
- **Конфигурация:** appsettings.json + IOptions<T>
- **DI:** Microsoft.Extensions.DependencyInjection
- **Сериализация:** System.Text.Json
- **Логирование:** Serilog
- **MCP Server:** JSON-RPC 2.0 через stdio (отдельный console app)
- **Локализация:** AKSoftware.Localization.MultiLanguages (ru-RU, en-US)
- **Тестирование:** xUnit + NSubstitute + Testcontainers
- **Развёртывание:** Docker (postgres + AGE + pgvector)

### Паттерн: как в CreatioDownloader3

Проект повторяет архитектуру `W:\GitHub\CreatioDownloader3`:
- **Client** (Blazor WASM) — UI на MudBlazor, вызывает Server API
- **Server** (ASP.NET Core) — API, Hangfire, бизнес-логика, БД
- **Shared** — DTO, интерфейсы сервисов, общие модели
- **InteractiveAuto** render mode — SSR при первой загрузке, затем WASM

---

## Архитектура

### Общая структура решения (.NET Solution)

```
TaskForesight.slnx
├── src/
│   ├── TaskForesight.Client/          # Blazor WebAssembly (UI)
│   ├── TaskForesight.Server/          # ASP.NET Core хост (API + Hangfire + Swagger)
│   ├── TaskForesight.Shared/          # DTO, интерфейсы, общие модели
│   └── TaskForesight.Core/            # Бизнес-логика (collector, processor, analytics)
├── tests/
│   ├── TaskForesight.Tests.Unit/
│   └── TaskForesight.Tests.Integration/
├── docker/
│   ├── docker-compose.yml
│   ├── Dockerfile.postgres
│   └── init.sql
└── prompts/
    ├── estimation.md
    └── risk_assessment.md
```

### Тестирование API
Swagger UI доступен в Development режиме: http://localhost:5000/swagger
AI-агенты используют тот же REST API что и все остальные клиенты.

---

## TaskForesight.Client (Blazor WebAssembly)

### Program.cs

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// HttpClient для вызова Server API
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Сервисы (клиентская реализация — через HttpClient)
builder.Services.AddScoped<IAnalyticsDataService, AnalyticsDataService>();
builder.Services.AddSingleton<DashboardCacheService>();

// MudBlazor
builder.Services.AddMudServices();

// Локализация
builder.Services.AddLanguageContainer(assembly: typeof(Program).Assembly);

await builder.Build().RunAsync();
```

### Страницы (Pages)

| Страница | Route | Описание |
|---|---|---|
| **Dashboard.razor** | `/` | Главный дашборд: ключевые метрики, графики, здоровье команды |
| **Estimate.razor** | `/estimate` | Интерактивная оценка задачи: ввод данных → AI-оценка |
| **TaskExplorer.razor** | `/tasks` | Список задач с метриками, фильтры, поиск |
| **TaskDetail.razor** | `/tasks/{key}` | Детали задачи: метрики, граф связей, история переходов |
| **GraphViewer.razor** | `/graph` | Интерактивная визуализация графа зависимостей |
| **SimilarSearch.razor** | `/similar` | Семантический поиск похожих задач |
| **TeamStats.razor** | `/team` | Статистика по разработчикам: точность оценок, bug rate |
| **SprintReport.razor** | `/sprint/{name}` | Отчёт по спринту |
| **DataCollection.razor** | `/admin/collect` | Управление сбором данных (запуск, статус, Hangfire) |
| **Settings.razor** | `/admin/settings` | Настройки: Jira, embeddings, категории, маппинг статусов |

### Пример: Dashboard.razor

```razor
@page "/"
@inject IAnalyticsDataService DataService
@inject DashboardCacheService Cache

<MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="mt-4">
    <MudText Typo="Typo.h4">Аналитика задач</MudText>

    @if (_loading)
    {
        <MudProgressCircular Indeterminate="true" />
    }
    else
    {
        <!-- KPI карточки -->
        <MudGrid Class="mt-4">
            <MudItem xs="12" sm="6" md="3">
                <MudCard>
                    <MudCardContent>
                        <MudText Typo="Typo.subtitle1">Средний Cycle Time</MudText>
                        <MudText Typo="Typo.h4" Color="Color.Primary">
                            @_stats.AvgCycleTimeDays.ToString("F1") дн
                        </MudText>
                    </MudCardContent>
                </MudCard>
            </MudItem>
            <MudItem xs="12" sm="6" md="3">
                <MudCard>
                    <MudCardContent>
                        <MudText Typo="Typo.subtitle1">Точность оценок</MudText>
                        <MudText Typo="Typo.h4" Color="@AccuracyColor(_stats.AvgEstimationAccuracy)">
                            @(_stats.AvgEstimationAccuracy * 100).ToString("F0")%
                        </MudText>
                    </MudCardContent>
                </MudCard>
            </MudItem>
            <MudItem xs="12" sm="6" md="3">
                <MudCard>
                    <MudCardContent>
                        <MudText Typo="Typo.subtitle1">Return Rate</MudText>
                        <MudText Typo="Typo.h4" Color="Color.Warning">
                            @(_stats.AvgReturnRate * 100).ToString("F0")%
                        </MudText>
                    </MudCardContent>
                </MudCard>
            </MudItem>
            <MudItem xs="12" sm="6" md="3">
                <MudCard>
                    <MudCardContent>
                        <MudText Typo="Typo.subtitle1">Пост-релизные баги</MudText>
                        <MudText Typo="Typo.h4" Color="Color.Error">
                            @_stats.AvgPostReleaseBugs.ToString("F1") / задачу
                        </MudText>
                    </MudCardContent>
                </MudCard>
            </MudItem>
        </MudGrid>

        <!-- Графики -->
        <MudGrid Class="mt-4">
            <MudItem xs="12" md="6">
                <MudPaper Class="pa-4">
                    <MudText Typo="Typo.h6">Точность оценок по категориям</MudText>
                    <MudChart ChartType="ChartType.Bar"
                              ChartSeries="@_accuracyByCategorySeries"
                              XAxisLabels="@_categoryLabels" />
                </MudPaper>
            </MudItem>
            <MudItem xs="12" md="6">
                <MudPaper Class="pa-4">
                    <MudText Typo="Typo.h6">Топ-10 "токсичных" задач</MudText>
                    <MudTable Items="@_toxicChains" Dense="true" Hover="true">
                        <HeaderContent>
                            <MudTh>Задача</MudTh>
                            <MudTh>Баги</MudTh>
                            <MudTh>Доп. время (ч)</MudTh>
                            <MudTh>Глубина</MudTh>
                        </HeaderContent>
                        <RowTemplate>
                            <MudTd><MudLink Href="@($"/tasks/{context.Key}")">@context.Key</MudLink></MudTd>
                            <MudTd>@context.TotalBugs</MudTd>
                            <MudTd>@context.TotalBugFixTime.ToString("F1")</MudTd>
                            <MudTd>@context.MaxChainDepth</MudTd>
                        </RowTemplate>
                    </MudTable>
                </MudPaper>
            </MudItem>
        </MudGrid>
    }
</MudContainer>
```

### Пример: Estimate.razor

```razor
@page "/estimate"
@inject IAnalyticsDataService DataService

<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-4">
    <MudText Typo="Typo.h4">Оценка задачи</MudText>

    <MudPaper Class="pa-4 mt-4">
        <MudTextField @bind-Value="_request.Summary" Label="Заголовок задачи"
                      Variant="Variant.Outlined" />
        <MudTextField @bind-Value="_request.Description" Label="Описание"
                      Lines="4" Variant="Variant.Outlined" Class="mt-2" />
        <MudSelect @bind-Value="_request.IssueType" Label="Тип задачи"
                   Variant="Variant.Outlined" Class="mt-2">
            <MudSelectItem Value="@("Story")">Story</MudSelectItem>
            <MudSelectItem Value="@("Task")">Task</MudSelectItem>
            <MudSelectItem Value="@("Bug")">Bug</MudSelectItem>
        </MudSelect>
        <MudAutocomplete @bind-Value="_request.Assignee" Label="Исполнитель"
                         SearchFunc="SearchDevelopers" Variant="Variant.Outlined" Class="mt-2" />
        <MudAutocomplete @bind-Value="_request.Component" Label="Компонент"
                         SearchFunc="SearchComponents" Variant="Variant.Outlined" Class="mt-2" />

        <MudButton Variant="Variant.Filled" Color="Color.Primary" Class="mt-4"
                   OnClick="EstimateAsync" Disabled="_estimating">
            @if (_estimating) { <MudProgressCircular Size="Size.Small" Indeterminate="true" /> }
            Оценить
        </MudButton>
    </MudPaper>

    @if (_estimation is not null)
    {
        <MudPaper Class="pa-4 mt-4">
            <MudText Typo="Typo.h5">Результат оценки</MudText>

            <MudSimpleTable Dense="true" Class="mt-2">
                <tbody>
                    <tr><td>Базовая оценка (медиана аналогов)</td>
                        <td><strong>@FormatHours(_estimation.BaseEstimateHours)</strong></td></tr>
                    <tr><td>Вероятность багов</td>
                        <td>@(_estimation.BugProbability * 100).ToString("F0")%</td></tr>
                    <tr><td>Скорректированная оценка</td>
                        <td><strong>@FormatHours(_estimation.AdjustedEstimateHours)</strong></td></tr>
                    <tr><td>Пессимистичная (P80)</td>
                        <td>@FormatHours(_estimation.PessimisticEstimateHours)</td></tr>
                    <tr><td>Уверенность</td>
                        <td>@(_estimation.Confidence * 100).ToString("F0")%</td></tr>
                </tbody>
            </MudSimpleTable>

            <MudText Typo="Typo.h6" Class="mt-4">Похожие задачи</MudText>
            <MudTable Items="@_estimation.SimilarTasks" Dense="true">
                <HeaderContent>
                    <MudTh>Задача</MudTh>
                    <MudTh>Реальная стоимость</MudTh>
                    <MudTh>Возвраты</MudTh>
                    <MudTh>Похожесть</MudTh>
                </HeaderContent>
                <RowTemplate>
                    <MudTd><MudLink Href="@($"/tasks/{context.Key}")">@context.Key</MudLink> @context.Summary</MudTd>
                    <MudTd>@FormatHours(context.RealCostHours)</MudTd>
                    <MudTd>@context.ReturnCount</MudTd>
                    <MudTd>@((1 - context.Distance) * 100).ToString("F0")%</MudTd>
                </RowTemplate>
            </MudTable>
        </MudPaper>
    }
</MudContainer>
```

### DashboardCacheService (паттерн из CreatioDownloader3)

```csharp
// Кэширование данных на клиенте (Singleton)
public class DashboardCacheService
{
    public DashboardStats? Stats { get; set; }
    public IReadOnlyList<CategoryStats>? Categories { get; set; }
    public IReadOnlyList<ToxicChain>? ToxicChains { get; set; }
    public IReadOnlyList<string>? Developers { get; set; }
    public IReadOnlyList<string>? Components { get; set; }

    public bool HasData => Stats is not null && Categories is not null;

    public void Clear()
    {
        Stats = null;
        Categories = null;
        ToxicChains = null;
        Developers = null;
        Components = null;
    }
}
```

### IAnalyticsDataService (в Shared, паттерн из CreatioDownloader3)

```csharp
// Shared — интерфейс, реализации в Client (HTTP) и Server (прямой доступ)
public interface IAnalyticsDataService
{
    // Dashboard
    Task<DashboardStats> GetDashboardStatsAsync();
    Task<IReadOnlyList<CategoryStats>> GetCategoryStatsAsync();
    Task<IReadOnlyList<ToxicChain>> GetToxicChainsAsync(int limit = 10);

    // Estimation
    Task<TaskEstimation> EstimateAsync(EstimationRequest request);
    Task<IReadOnlyList<SimilarTask>> FindSimilarAsync(string text, string? issueType = null, int limit = 10);

    // Tasks
    Task<PagedResult<TaskSummaryDto>> GetTasksAsync(TaskFilter filter);
    Task<TaskDetailDto?> GetTaskDetailAsync(string key);

    // Team
    Task<IReadOnlyList<DeveloperStats>> GetDeveloperStatsAsync();
    Task<DeveloperDetailDto?> GetDeveloperDetailAsync(string name);

    // Graph
    Task<GraphData> GetTaskGraphAsync(string key, int depth = 3);
    Task<IReadOnlyList<CrossComponentRisk>> GetCrossComponentRisksAsync(string component);

    // Admin
    Task<CollectionStatus> GetCollectionStatusAsync();
    Task StartCollectionAsync(CollectionRequest request);

    // Autocomplete
    Task<IReadOnlyList<string>> GetDevelopersAsync();
    Task<IReadOnlyList<string>> GetComponentsAsync();
}
```

**Две реализации:**

```csharp
// Client — через HttpClient (как CreatioDataService в CreatioDownloader3)
public class AnalyticsDataService : IAnalyticsDataService
{
    private readonly HttpClient _http;

    public async Task<DashboardStats> GetDashboardStatsAsync()
        => await _http.GetFromJsonAsync<DashboardStats>("api/analytics/dashboard")
           ?? new DashboardStats();

    public async Task<TaskEstimation> EstimateAsync(EstimationRequest request)
        => await _http.PostAsJsonAsync("api/analytics/estimate", request)
           .ContinueWith(t => t.Result.Content.ReadFromJsonAsync<TaskEstimation>()).Unwrap()
           ?? throw new InvalidOperationException("Estimation failed");
    // ...
}

// Server — прямой доступ к сервисам (как ServerCreatioDataService)
public class ServerAnalyticsDataService : IAnalyticsDataService
{
    private readonly IEstimationService _estimation;
    private readonly ITaskRepository _tasks;
    private readonly IGraphRepository _graph;
    // ... прямые вызовы без HTTP
}
```

---

## TaskForesight.Server (ASP.NET Core)

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Razor Components (Blazor InteractiveAuto)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// MudBlazor
builder.Services.AddMudServices();

// PostgreSQL — единый DataSource для SQL + Graph (AGE) + Vector (pgvector)
var dataSourceBuilder = new NpgsqlDataSourceBuilder(
    builder.Configuration.GetConnectionString("Analytics")!);
dataSourceBuilder.UseAge();       // Konnektr.Npgsql.Age — Apache AGE графы
dataSourceBuilder.UseVector();    // Pgvector — векторные embeddings
await using var dataSource = dataSourceBuilder.Build();
builder.Services.AddSingleton(dataSource);

// Repositories
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IVectorRepository, VectorRepository>();
builder.Services.AddScoped<IGraphRepository, GraphRepository>();

// Business services
builder.Services.AddScoped<IEstimationService, EstimationService>();
builder.Services.AddScoped<IRiskAnalyzer, RiskAnalyzer>();
builder.Services.AddScoped<ISimilarityService, SimilarityService>();

// Jira collector
builder.Services.AddHttpClient<IJiraClient, JiraClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<JiraOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    var auth = Convert.ToBase64String(
        Encoding.UTF8.GetBytes($"{options.Username}:{options.Credential}"));
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Basic", auth);
    client.Timeout = options.RequestTimeout;
}).AddPolicyHandler(GetRetryPolicy()); // Polly

builder.Services.AddScoped<IBatchCollector, BatchCollector>();
builder.Services.AddScoped<IDataProcessor, DataProcessor>();
builder.Services.AddScoped<IEmbeddingGenerator, EmbeddingGenerator>();

// Hangfire (фоновые задачи: сбор данных, пересчёт)
builder.Services.AddHangfire(config =>
    config.UseInMemoryStorage()); // или UsePostgreSqlStorage
builder.Services.AddHangfireServer();

// Server-side data service (для SSR)
builder.Services.AddScoped<IAnalyticsDataService, ServerAnalyticsDataService>();
builder.Services.AddSingleton<DashboardCacheService>();

// Configuration
builder.Services.Configure<JiraOptions>(builder.Configuration.GetSection("Jira"));
builder.Services.Configure<EmbeddingOptions>(builder.Configuration.GetSection("Embedding"));
builder.Services.Configure<AnalyticsOptions>(builder.Configuration.GetSection("Analytics"));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseAntiforgery();

// API endpoints
app.MapAnalyticsApi();       // /api/analytics/*
app.MapCollectionApi();      // /api/collection/*

// Hangfire dashboard
app.MapHangfireDashboard("/hangfire");

// Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(TaskForesight.Client._Imports).Assembly);

// Hangfire recurring jobs
RecurringJob.AddOrUpdate<IDataProcessor>(
    "incremental-collect",
    x => x.RunIncrementalAsync(CancellationToken.None),
    Cron.Daily(3, 0)); // каждый день в 03:00

RecurringJob.AddOrUpdate<ITaskRepository>(
    "refresh-materialized-views",
    x => x.RefreshMaterializedViewsAsync(CancellationToken.None),
    Cron.Daily(4, 0)); // каждый день в 04:00

app.Run();
```

### API Endpoints (Minimal API)

```csharp
public static class AnalyticsApiExtensions
{
    public static void MapAnalyticsApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/analytics");

        group.MapGet("/dashboard", async (IAnalyticsDataService svc) =>
            Results.Ok(await svc.GetDashboardStatsAsync()));

        group.MapGet("/categories", async (IAnalyticsDataService svc) =>
            Results.Ok(await svc.GetCategoryStatsAsync()));

        group.MapGet("/toxic-chains", async (IAnalyticsDataService svc, int limit = 10) =>
            Results.Ok(await svc.GetToxicChainsAsync(limit)));

        group.MapPost("/estimate", async (IAnalyticsDataService svc, EstimationRequest req) =>
            Results.Ok(await svc.EstimateAsync(req)));

        group.MapGet("/similar", async (IAnalyticsDataService svc, string text, string? type, int limit = 10) =>
            Results.Ok(await svc.FindSimilarAsync(text, type, limit)));

        group.MapGet("/tasks", async (IAnalyticsDataService svc, [AsParameters] TaskFilter filter) =>
            Results.Ok(await svc.GetTasksAsync(filter)));

        group.MapGet("/tasks/{key}", async (IAnalyticsDataService svc, string key) =>
            Results.Ok(await svc.GetTaskDetailAsync(key)));

        group.MapGet("/team", async (IAnalyticsDataService svc) =>
            Results.Ok(await svc.GetDeveloperStatsAsync()));

        group.MapGet("/team/{name}", async (IAnalyticsDataService svc, string name) =>
            Results.Ok(await svc.GetDeveloperDetailAsync(name)));

        group.MapGet("/graph/{key}", async (IAnalyticsDataService svc, string key, int depth = 3) =>
            Results.Ok(await svc.GetTaskGraphAsync(key, depth)));

        group.MapGet("/cross-risks/{component}", async (IAnalyticsDataService svc, string component) =>
            Results.Ok(await svc.GetCrossComponentRisksAsync(component)));

        group.MapGet("/developers", async (IAnalyticsDataService svc) =>
            Results.Ok(await svc.GetDevelopersAsync()));

        group.MapGet("/components", async (IAnalyticsDataService svc) =>
            Results.Ok(await svc.GetComponentsAsync()));
    }

    public static void MapCollectionApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/collection");

        group.MapGet("/status", async (IAnalyticsDataService svc) =>
            Results.Ok(await svc.GetCollectionStatusAsync()));

        group.MapPost("/start", async (IAnalyticsDataService svc, CollectionRequest req) =>
        {
            await svc.StartCollectionAsync(req);
            return Results.Accepted();
        });
    }
}
```

### App.razor (паттерн из CreatioDownloader3)

```razor
<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Jira Task Analytics</title>
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <HeadOutlet @rendermode="InteractiveAuto" />
</head>
<body>
    <Routes @rendermode="InteractiveAuto" />
    <script src="_framework/blazor.web.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
</body>
</html>
```

### appsettings.json

```json
{
  "ConnectionStrings": {
    "Analytics": "Host=localhost;Port=5432;Database=jira_analytics;Username=analytics"
  },
  "Jira": {
    "BaseUrl": "https://boards.banzait.com",
    "Username": "",
    "MaxComments": 10,
    "IncludeLinked": true,
    "MaxConcurrentRequests": 4,
    "RequestTimeout": "00:00:15"
  },
  "LlmProxy": {
    "BaseUrl": "http://localhost:8080/v1",
    "ApiKey": "",
    "Model": "gemini-2.5-pro"
  },
  "Embedding": {
    "Provider": "gemini",
    "ApiUrl": "https://generativelanguage.googleapis.com/v1beta",
    "ApiKey": "",
    "Model": "text-embedding-004",
    "Dimensions": 768
  },
  "Analytics": {
    "DefaultHistoryMonths": 6,
    "SimilarTasksLimit": 10
  }
}
```

---

## TaskForesight.Shared (DTO и интерфейсы)

```csharp
// DTO для API
public record DashboardStats(
    double AvgCycleTimeDays,
    double AvgEstimationAccuracy,
    double AvgReturnRate,
    double AvgPostReleaseBugs,
    int TotalTasks,
    int TotalBugs,
    DateTimeOffset LastCollectedAt);

public record CategoryStats(
    string Category,
    int SampleCount,
    double AvgCycleTime,
    double MedianCycleTime,
    double AvgEstimationAccuracy,
    double AvgReturnRate,
    double AvgPostReleaseBugs,
    double MedianRealCost,
    double P80RealCost);

public record EstimationRequest(
    string Summary,
    string? Description,
    string IssueType,
    string? Assignee,
    string? Component);

public record TaskEstimation(
    double BaseEstimateHours,
    double PessimisticEstimateHours,
    double BugProbability,
    double ExpectedReturns,
    double AdjustedEstimateHours,
    IReadOnlyList<CrossComponentRisk> CrossComponentRisks,
    IReadOnlyList<SimilarTask> SimilarTasks,
    double Confidence);

public record SimilarTask(string Key, string Summary, double RealCostHours, int ReturnCount, double CycleTime, double Distance);
public record ToxicChain(string Key, string Summary, int TotalBugs, double TotalBugFixTime, int MaxChainDepth);
public record DeveloperStats(string Name, int Tasks, double AvgAccuracy, double BugRate, double AvgCycleTime);
public record CrossComponentRisk(string ComponentName, int CrossBugs);
public record CollectionStatus(bool IsRunning, DateTimeOffset? LastRun, int TotalCollected, string? CurrentJql);
public record CollectionRequest(string? Jql, DateTimeOffset? Since, bool RebuildGraph, bool RebuildEmbeddings);

public record TaskSummaryDto(string Key, string Summary, string IssueType, string? Assignee, double? CycleTime, double? RealCostHours, int ReturnCount, DateTimeOffset? ResolvedAt);
public record TaskDetailDto(string Key, string Summary, string? Description, string IssueType, string? Assignee, string? Reporter, IReadOnlyList<string> Components, double? CycleTime, double? RealCostHours, double? EstimationAccuracy, int ReturnCount, int DirectBugsCount, int PostReleaseBugsCount, IReadOnlyList<StatusTransitionDto> Transitions, IReadOnlyList<TaskLinkDto> Links);
public record StatusTransitionDto(string FromStatus, string ToStatus, string Author, DateTimeOffset TransitionedAt, double DurationHours);
public record TaskLinkDto(string TargetKey, string TargetSummary, string LinkType, string TargetIssueType);

public record GraphData(IReadOnlyList<GraphNode> Nodes, IReadOnlyList<GraphEdge> Edges);
public record GraphNode(string Id, string Label, string Type, Dictionary<string, object>? Properties);
public record GraphEdge(string Source, string Target, string Type, Dictionary<string, object>? Properties);

public record TaskFilter(string? IssueType, string? Assignee, string? Category, string? Component, string? Search, int Page = 1, int PageSize = 25);
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
```

---

## TaskForesight.Core (бизнес-логика)

### Collector

```csharp
public class JiraClient : IJiraClient { /* HttpClient + Basic Auth, retry с Polly */ }
public class BatchCollector : IBatchCollector { /* IAsyncEnumerable, pagination, rate limiting */ }
public class LinkResolver : ILinkResolver { /* рекурсивный обход issuelinks, пост-релизные баги */ }
public class ChangelogParser : IChangelogParser { /* JSON changelog → StatusTransition[] */ }
```

### Processor

```csharp
public class TimeCalculator : ITimeCalculator { /* время в каждом статусе */ }
public class ReturnDetector : IReturnDetector { /* возвраты testing → in progress */ }
public class CostCalculator : ICostCalculator { /* реальная стоимость задачи */ }
public class TaskClassifier : ITaskClassifier { /* категоризация по компонентам/меткам */ }
public class EmbeddingGenerator : IEmbeddingGenerator { /* OpenAI-совместимый API через CLIProxyAPIPlus */ }
public class GraphBuilder : IGraphBuilder { /* построение узлов + рёбер в AGE */ }
public class DataProcessor : IDataProcessor { /* оркестратор: collect → process → store */ }
```

### Storage

```csharp
public class TaskRepository : ITaskRepository { /* Npgsql + Dapper, SQL queries */ }
public class VectorRepository : IVectorRepository { /* pgvector similarity search */ }
public class GraphRepository : IGraphRepository { /* AGE Cypher через Konnektr.Npgsql.Age (CreateCypherCommand + параметры) */ }
```

### Analytics

```csharp
public class EstimationService : IEstimationService { /* vector + graph + sql = оценка */ }
public class RiskAnalyzer : IRiskAnalyzer { /* graph traversal для оценки рисков */ }
public class SimilarityService : ISimilarityService { /* vector + graph context */ }
public class ReportGenerator : IReportGenerator { /* markdown отчёты */ }
```

---

## TaskForesight.McpServer (console app для AI)

Отдельный console app — подключается из любого MCP-совместимого клиента (Claude Code, AI-агенты, и т.д.).

```csharp
// Program.cs
var builder = Host.CreateApplicationBuilder(args);
var dsBuilder = new NpgsqlDataSourceBuilder(
    builder.Configuration.GetConnectionString("Analytics")!);
dsBuilder.UseAge();
dsBuilder.UseVector();
builder.Services.AddSingleton(dsBuilder.Build());
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IVectorRepository, VectorRepository>();
builder.Services.AddScoped<IGraphRepository, GraphRepository>();
builder.Services.AddScoped<IEstimationService, EstimationService>();
builder.Services.AddSingleton<McpServer>();

var host = builder.Build();
await host.Services.GetRequiredService<McpServer>().RunAsync();
```

**MCP Resources:**

```
analytics://estimate/{issue_type}/{category}
analytics://similar/{key}
analytics://team/{assignee}/stats
analytics://risk/{key}
analytics://report/sprint/{sprint}
```

---

## PostgreSQL Multi-Model — схема БД

### Docker (в WSL дистрибутиве BanzaWork)

Docker установлен в WSL, имя дистрибутива: **BanzaWork**.

Запуск из Windows:
```bash
wsl -d BanzaWork -- docker compose -f /path/to/docker-compose.yml up -d
```

Или из WSL терминала:
```bash
wsl -d BanzaWork
cd /mnt/w/GitHub/TaskForesight/docker
docker compose up -d
```

```yaml
# docker/docker-compose.yml
services:
  postgres:
    image: apache/age:v1.5.0-pg16
    ports:
      - "5432:5432"
    environment:
      POSTGRES_DB: jira_analytics
      POSTGRES_USER: analytics
    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql
    command: >
      postgres -c shared_preload_libraries='age,vector'

volumes:
  pgdata:
```

Подключение из .NET (Windows) к PostgreSQL в WSL:
```
Host=localhost;Port=5432;Database=jira_analytics;Username=analytics
```

### SQL схема (init.sql)

```sql
CREATE EXTENSION IF NOT EXISTS age;
CREATE EXTENSION IF NOT EXISTS vector;
LOAD 'age';
SET search_path = ag_catalog, "$user", public;
SELECT create_graph('jira_graph');

CREATE TABLE tasks (
    key TEXT PRIMARY KEY,
    summary TEXT,
    description TEXT,
    issue_type TEXT,
    priority TEXT,
    status TEXT,
    assignee TEXT,
    reporter TEXT,
    components JSONB,
    labels JSONB,
    created_at TIMESTAMPTZ,
    resolved_at TIMESTAMPTZ,
    time_in_open REAL,
    time_in_progress REAL,
    time_in_code_review REAL,
    time_in_testing REAL,
    cycle_time REAL,
    lead_time REAL,
    original_estimate_hours REAL,
    time_spent_hours REAL,
    estimation_accuracy REAL,
    return_count INTEGER DEFAULT 0,
    reopen_count INTEGER DEFAULT 0,
    direct_bugs_count INTEGER DEFAULT 0,
    post_release_bugs_count INTEGER DEFAULT 0,
    bug_fix_time_hours REAL DEFAULT 0,
    real_cost_hours REAL,
    task_category TEXT,
    embedding vector(1536),
    collected_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_tasks_embedding ON tasks USING hnsw (embedding vector_cosine_ops) WITH (m = 16, ef_construction = 64);
CREATE INDEX idx_tasks_type ON tasks(issue_type);
CREATE INDEX idx_tasks_category ON tasks(task_category);
CREATE INDEX idx_tasks_assignee ON tasks(assignee);
CREATE INDEX idx_tasks_resolved ON tasks(resolved_at);
CREATE INDEX idx_tasks_components ON tasks USING gin(components);

CREATE TABLE status_transitions (
    id SERIAL PRIMARY KEY,
    task_key TEXT REFERENCES tasks(key),
    from_status TEXT,
    to_status TEXT,
    author TEXT,
    transitioned_at TIMESTAMPTZ,
    duration_hours REAL
);
CREATE INDEX idx_transitions_task ON status_transitions(task_key);

CREATE MATERIALIZED VIEW category_stats AS
SELECT task_category AS category, COUNT(*) AS sample_count,
    AVG(cycle_time) AS avg_cycle_time,
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY cycle_time) AS median_cycle_time,
    AVG(estimation_accuracy) AS avg_estimation_accuracy,
    AVG(post_release_bugs_count) AS avg_post_release_bugs,
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY real_cost_hours) AS median_real_cost,
    PERCENTILE_CONT(0.8) WITHIN GROUP (ORDER BY real_cost_hours) AS p80_real_cost
FROM tasks WHERE resolved_at IS NOT NULL AND resolved_at >= NOW() - INTERVAL '6 months'
GROUP BY task_category;
```

### Граф (Apache AGE) — узлы и рёбра

```
Узлы: :Task, :Developer, :Component, :Sprint, :Version
Рёбра: :CAUSED_BY, :RELATES_TO, :BLOCKS, :SUBTASK_OF, :ASSIGNED, :REPORTED,
       :BELONGS_TO, :IN_SPRINT, :FIX_VERSION, :FOUND_AFTER_CLOSE
```

---

## Hangfire — фоновые задачи

| Job | Расписание | Описание |
|---|---|---|
| incremental-collect | Daily 03:00 | Инкрементальный сбор из Jira (updated >= last_run) |
| refresh-views | Daily 04:00 | REFRESH MATERIALIZED VIEW category_stats |
| rebuild-embeddings | Weekly Sun 02:00 | Пересоздание embeddings для изменённых задач |
| anomaly-detection | Daily 05:00 | Поиск аномалий (резкие изменения метрик) |

Также доступен ручной запуск через UI: `/admin/collect`.

---

## Процессы

### Процесс 1: Первичный сбор данных

```
1. docker-compose up (PostgreSQL + AGE + pgvector)
2. Открыть /admin/collect или POST /api/collection/start
3. Hangfire job:
   a. JQL: все закрытые задачи за N месяцев
   b. Для каждой: issue + changelog + comments + links + subtasks
   c. Поиск пост-релизных багов
   d. Сохранение сырых JSON в data/raw/
4. Обработка:
   a. Вычислить метрики
   b. Записать в SQL-таблицы
   c. Построить граф в AGE
   d. Сгенерировать embeddings
5. REFRESH MATERIALIZED VIEW
```

### Процесс 2: Инкрементальный (автоматический)

```
Hangfire Daily 03:00:
1. JQL: updated >= last_run_date
2. Обновить SQL + граф + embeddings
3. Refresh views
4. Уведомление об аномалиях (если есть)
```

### Процесс 3: Оценка задачи (через UI)

```
1. Пользователь открывает /estimate
2. Вводит summary, description, тип, исполнитель, компонент
3. POST /api/analytics/estimate
4. Server:
   a. Генерирует embedding
   b. VECTOR: 10 похожих задач
   c. SQL: агрегация
   d. GRAPH: риски
5. Возвращает оценку с обоснованием
```

### Процесс 4: Запрос аналитики через MCP (для AI-агентов)

```
AI-агент (Claude Code, или другой MCP-совместимый клиент):
1. Подключается к TaskForesight.McpServer (stdio)
2. Запрашивает resources/list → видит доступные ресурсы
3. Запрашивает resources/read → analytics://estimate/Story/integration
4. MCP Server:
   a. EstimationService выполняет запросы (VECTOR + SQL + GRAPH)
   b. Форматирует результат в markdown
5. AI получает markdown с оценкой, рисками, похожими задачами
6. Использует данные для ответа пользователю
```

### Процесс 5: Запрос аналитики через REST API (для внешних систем)

```
Любой внешний клиент (скрипт, бот, дашборд, Telegram-бот):
1. GET /api/analytics/estimate?summary=...&issueType=Story&component=...
2. Server возвращает JSON с оценкой
3. Клиент отображает/использует данные

Примеры интеграций:
- Telegram-бот для быстрых оценок задач
- Jira webhook → автоматическая оценка при создании задачи
- CI/CD pipeline → проверка реалистичности оценки перед началом спринта
- Будущая интеграция с ai-review (отдельный проект)
```

---

## NuGet пакеты

```xml
<!-- Client -->
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" />
<PackageReference Include="MudBlazor" />
<PackageReference Include="AKSoftware.Localization.MultiLanguages" />
<PackageReference Include="AKSoftware.Localization.MultiLanguages.Blazor" />

<!-- Server / Core -->
<PackageReference Include="Npgsql" />
<PackageReference Include="Dapper" />
<PackageReference Include="Pgvector" />                        <!-- pgvector: UseVector(), маппинг vector типов -->
<PackageReference Include="Konnektr.Npgsql.Age" />             <!-- Apache AGE: UseAge(), CreateCypherCommand(), Agtype, Vertex, Edge -->
<PackageReference Include="Hangfire.AspNetCore" />
<PackageReference Include="Hangfire.InMemory" />
<PackageReference Include="Polly.Extensions.Http" />
<PackageReference Include="Serilog.AspNetCore" />
<PackageReference Include="Swashbuckle.AspNetCore" />
<PackageReference Include="MudBlazor" />

<!-- Testing -->
<PackageReference Include="xunit" />
<PackageReference Include="NSubstitute" />
<PackageReference Include="Testcontainers.PostgreSql" />
<PackageReference Include="FluentAssertions" />
```

---

## Задачи на реализацию

### Этап 1: Инфраструктура ✅
- [x] Создать репозиторий TaskForesight (GitHub)
- [x] dotnet new slnx + 4 проекта (Client, Server, Shared, Core)
- [x] Docker: PostgreSQL 16 + AGE + pgvector
- [x] init.sql: расширения, граф, таблицы, индексы
- [x] MudBlazor + InteractiveAuto setup (по паттерну CreatioDownloader3)
- [x] appsettings.json + IOptions
- [x] Swagger на Server (http://localhost:5000/swagger)

### Этап 2: Core — Jira коллектор ✅
- [x] JiraClient (HttpClient + Basic Auth)
- [x] BatchCollector (IAsyncEnumerable, pagination)
- [x] ChangelogParser (status transitions, return detection)
- [x] LinkResolver (issuelinks, пост-релизные баги)
- [x] Тестовый API endpoint /api/test/collect

### Этап 3: Core — процессор + хранение
- [ ] Docker: поднять PostgreSQL + AGE + pgvector
- [ ] TaskRepository (Npgsql + Dapper) — запись задач в БД
- [ ] TimeCalculator — время в каждом статусе
- [ ] CostCalculator — реальная стоимость задачи
- [ ] TaskClassifier — категоризация по компонентам/меткам
- [ ] DataProcessor — оркестратор: collect → process → store
- [ ] Hangfire jobs (сбор, refresh materialized views)

### Этап 4: Embeddings + граф + аналитика
- [ ] EmbeddingGenerator (Gemini API)
- [ ] VectorRepository (pgvector — семантический поиск)
- [ ] GraphBuilder + GraphRepository (AGE Cypher)
- [ ] EstimationService (vector + graph + sql = оценка)
- [ ] RiskAnalyzer, SimilarityService

### Этап 5: Server — полный API
- [ ] Minimal API endpoints (/api/analytics/*, /api/collection/*)
- [ ] ServerAnalyticsDataService — реализация интерфейса через Core
- [ ] AnalyticsDataService (Client — HTTP клиент)

### Этап 6: Client — UI
- [ ] Dashboard.razor (KPI карточки + графики)
- [ ] Estimate.razor (форма + AI-оценка + похожие задачи)
- [ ] TaskExplorer.razor (таблица + фильтры)
- [ ] TaskDetail.razor (метрики + граф + история переходов)
- [ ] GraphViewer.razor (визуализация графа зависимостей)
- [ ] TeamStats.razor (статистика разработчиков)
- [ ] DataCollection.razor (запуск/статус сбора данных)
- [ ] Settings.razor (Jira, embeddings, маппинг статусов)

### Этап 7: Тесты + доводка
- [ ] Unit-тесты (calculators, parsers, classifier)
- [ ] Integration-тесты (Testcontainers)
- [ ] Обработка ошибок и edge cases
- [ ] Промпты для AI-анализа (estimation.md, risk_assessment.md)

---

## Где взять код для примера

### Blazor WebAssembly + MudBlazor (паттерн проекта)
**Проект:** `W:\GitHub\CreatioDownloader3`
**Что взять:**
- `CreatioDownloader.Client/Program.cs` — настройка WASM клиента, DI, HttpClient
- `CreatioDownloader.Server/Program.cs` — InteractiveAuto, DI, Hangfire, API
- `CreatioDownloader.Server/App.razor` — точка входа Blazor
- `CreatioDownloader.Shared/Services/ICreatioDataService.cs` — паттерн интерфейса сервиса данных
- `CreatioDownloader.Client/Services/DataCacheService.cs` — клиентский кэш (Singleton)
- `CreatioDownloader.Client/Pages/` — примеры страниц с MudBlazor
- `CreatioDownloader2BlazorCore/` — паттерн выноса бизнес-логики в отдельный проект

### Логика Jira API (референс, переписать на C#)
**Файл:** `W:\GitHub\ai-review\jira_mcp_server.py`
**Что взять:**
- HTTP сессия с Basic Auth (строки 67-83) → HttpClient + DelegatingHandler
- Получение задачи (строки 125-158) → JiraClient.GetIssueAsync()
- Получение комментариев (строки 160-184) → JiraClient.GetCommentsAsync()
- Markdown формат (строки 186-268) → MarkdownFormatter
- Парсинг ключей (строки 86-123) → KeyParser

### Логика MCP протокола (референс, переписать на C#)
**Файл:** `W:\GitHub\ai-review\jira_mcp_server.py`
**Что взять:**
- MCP handshake (строки 272-289) → McpServer.HandleInitialize()
- Роутер методов (строки 449-488) → McpServer.HandleRequestAsync()
- Главный цикл stdin/stdout (строки 490-526) → McpServer.RunAsync()

### MCP Client
**Файл:** `W:\GitHub\ai-review\ai_review\libs\mcp\client.py`

### Apache AGE .NET клиент
**NuGet:** `Konnektr.Npgsql.Age` v1.2.1 — форк pg-age, улучшенный
**Репозиторий:** https://github.com/konnektr-io/npgsql-age
**Что даёт:**
- `UseAge()` — нативная интеграция с NpgsqlDataSourceBuilder (один DataSource для SQL + Graph + Vector)
- `CreateCypherCommand()` — типизированные Cypher-запросы с параметрами (Dictionary / JSON)
- `Agtype`, `Vertex`, `Edge` — маппинг agtype в C# объекты
- .NET 8.0+ target, 171 коммит, используется в production (Konnektr.AgeDigitalTwins, 40K downloads)
- Лицензия: Apache 2.0

**Альтернативы (для справки):**
- `ApacheAGE` v1.0.0 (https://github.com/Allison-E/pg-age) — оригинал, .NET 5.0+, свой AgeClientBuilder (не интегрирован с NpgsqlDataSource)
- Raw SQL через Npgsql — без зависимостей, но ручной парсинг agtype

### PostgreSQL Multi-Model подход
**Статья-референс:** "PostgreSQL Goes Multi-Model: Graph, Vector, and SQL" by Sixing Huang
- Apache AGE: Cypher в PostgreSQL
- pgvector: векторные embeddings
- Комбинированные запросы: SQL + Cypher + Vector в одном CTE

---

## Структура каталогов

```
TaskForesight/
├── TaskForesight.sln
├── docker/
│   ├── docker-compose.yml
│   ├── Dockerfile.postgres
│   └── init.sql
├── src/
│   ├── TaskForesight.Client/              # Blazor WebAssembly
│   │   ├── Program.cs
│   │   ├── _Imports.razor
│   │   ├── Layout/
│   │   │   └── MainLayout.razor               # MudLayout + NavMenu
│   │   ├── Pages/
│   │   │   ├── Dashboard.razor                # Главный дашборд
│   │   │   ├── Estimate.razor                 # Оценка задачи
│   │   │   ├── TaskExplorer.razor             # Список задач
│   │   │   ├── TaskDetail.razor               # Детали задачи + граф
│   │   │   ├── GraphViewer.razor              # Визуализация графа
│   │   │   ├── SimilarSearch.razor            # Семантический поиск
│   │   │   ├── TeamStats.razor                # Статистика команды
│   │   │   ├── SprintReport.razor             # Отчёт по спринту
│   │   │   ├── DataCollection.razor           # Управление сбором
│   │   │   └── Settings.razor                 # Настройки
│   │   ├── Services/
│   │   │   ├── AnalyticsDataService.cs        # HTTP клиент → Server API
│   │   │   └── DashboardCacheService.cs       # Клиентский кэш
│   │   └── wwwroot/
│   ├── TaskForesight.Server/              # ASP.NET Core хост
│   │   ├── Program.cs                         # DI, Blazor, API, Hangfire
│   │   ├── App.razor                          # InteractiveAuto entry
│   │   ├── Api/
│   │   │   ├── AnalyticsApiExtensions.cs      # /api/analytics/*
│   │   │   └── CollectionApiExtensions.cs     # /api/collection/*
│   │   ├── Services/
│   │   │   └── ServerAnalyticsDataService.cs  # Прямой доступ к Core
│   │   ├── Jobs/
│   │   │   └── HangfireJobs.cs                # Recurring jobs
│   │   └── appsettings.json
│   ├── TaskForesight.Shared/              # DTO, интерфейсы
│   │   ├── Dto/                               # Все record DTO
│   │   └── Services/
│   │       └── IAnalyticsDataService.cs       # Общий интерфейс
│   ├── TaskForesight.Core/                # Бизнес-логика
│   │   ├── Collector/
│   │   │   ├── JiraClient.cs
│   │   │   ├── BatchCollector.cs
│   │   │   ├── LinkResolver.cs
│   │   │   └── ChangelogParser.cs
│   │   ├── Processor/
│   │   │   ├── TimeCalculator.cs
│   │   │   ├── ReturnDetector.cs
│   │   │   ├── CostCalculator.cs
│   │   │   ├── TaskClassifier.cs
│   │   │   ├── EmbeddingGenerator.cs
│   │   │   ├── GraphBuilder.cs
│   │   │   └── DataProcessor.cs
│   │   ├── Storage/
│   │   │   ├── TaskRepository.cs
│   │   │   ├── VectorRepository.cs
│   │   │   └── GraphRepository.cs
│   │   ├── Analytics/
│   │   │   ├── EstimationService.cs
│   │   │   ├── RiskAnalyzer.cs
│   │   │   ├── SimilarityService.cs
│   │   │   └── ReportGenerator.cs
│   │   ├── Interfaces/                        # Все I* интерфейсы
│   │   └── Options/                           # JiraOptions, EmbeddingOptions...
│   └── TaskForesight.McpServer/           # MCP для AI (console app)
│       ├── Program.cs
│       ├── McpServer.cs
│       └── ResourceHandlers/
├── tests/
│   ├── TaskForesight.Tests.Unit/
│   └── TaskForesight.Tests.Integration/
├── data/
│   └── raw/
└── prompts/
    ├── estimation.md
    └── risk_assessment.md
```

---

## Маппинг статусов Jira → метрики

Workflow в Jira может отличаться между проектами. Для расчёта метрик нужен маппинг статусов на стандартные категории:

```json
{
  "statusMapping": {
    "open":        ["Open", "New", "To Do", "Backlog", "Reopened"],
    "in_progress": ["In Progress", "In Development", "Coding"],
    "code_review": ["Code Review", "Review", "In Review", "PR Review"],
    "testing":     ["Testing", "QA", "In QA", "Verification", "In Testing"],
    "done":        ["Done", "Closed", "Resolved", "Released"]
  }
}
```

Маппинг настраивается через Settings UI (/admin/settings) и хранится в appsettings.json или в БД.

Детектор возвратов: переход из категории с бОльшим порядком в категорию с меньшим (testing → in_progress = возврат).

---

## Аутентификация Web UI

Для первой версии — **без аутентификации** (внутренний инструмент в корпоративной сети).

Будущее: добавить через Microsoft.AspNetCore.Authentication (LDAP/AD или OAuth через корпоративный SSO).

---

## Обработка ошибок

- **Jira недоступна (403/timeout):** коллектор логирует ошибку, пропускает задачу, продолжает сбор. Hangfire retry через 30 мин
- **Embedding API недоступна:** задача сохраняется без embedding, помечается для повторной генерации
- **AGE граф не синхронизирован:** команда `dotnet run -- graph --rebuild` пересоздаёт граф из SQL-данных
- **Нет данных для оценки:** EstimationService возвращает `Confidence = 0` и сообщение "Недостаточно данных"

---

## С чего начать (порядок для LLM)

1. **docker-compose up** — поднять PostgreSQL + AGE + pgvector
2. **dotnet new sln** + создать 5 проектов с зависимостями между ними
3. **init.sql** — создать схему БД
4. **JiraClient** — первый рабочий код: подключиться к Jira, получить одну задачу
5. **TimeCalculator** + **ChangelogParser** — обработать changelog → метрики
6. **TaskRepository** — записать в PostgreSQL
7. **Dashboard.razor** — отобразить хотя бы одну метрику в UI
8. Далее итеративно: коллектор → процессор → аналитика → UI → MCP

---

## Риски и ограничения

1. **Jira доступ (403)** — учётка Bnz_AI_CodeReview заблокирована, нужны права
2. **Rate limiting** — Jira может ограничивать запросы, нужен Polly throttling
3. **Объём данных** — первичный сбор тысяч задач может занять часы (Hangfire background)
4. **Качество данных** — не все заполняют оценки и логируют время
5. **Статусы** — workflow различается между проектами, нужна маппинг-таблица в Settings
6. **Приватность** — данные о производительности чувствительны
7. **Konnektr.Npgsql.Age** — community-пакет (4 stars, но 171 коммит, используется в production AgeDigitalTwins 40K downloads). Apache 2.0 — можно форкнуть при необходимости. Альтернативы: ApacheAGE NuGet (оригинал, .NET 5+) или raw SQL через Npgsql
8. **Совместимость Npgsql версий** — Konnektr.Npgsql.Age и Pgvector оба зависят от Npgsql, проверить что версии совместимы
9. **Docker-образ** — нужен кастомный Dockerfile (AGE + pgvector в одном)
10. **Стоимость embeddings** — CLIProxyAPIPlus не поддерживает embeddings, нужен прямой доступ к API. Варианты: Gemini text-embedding-004 (бесплатно), OpenAI (~$0.02/1M токенов), локальная модель (бесплатно, но нужен GPU/CPU)
11. **Blazor WASM размер** — первая загрузка может быть тяжёлой, InteractiveAuto смягчает это (SSR first)
