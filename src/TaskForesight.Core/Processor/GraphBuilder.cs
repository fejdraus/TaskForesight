using System.Text.Json;
using Microsoft.Extensions.Logging;
using Npgsql;
using TaskForesight.Core.Interfaces;
using TaskForesight.Core.Models;

namespace TaskForesight.Core.Processor;

public class GraphBuilder : IGraphBuilder
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<GraphBuilder> _logger;

    public GraphBuilder(NpgsqlDataSource dataSource, ILogger<GraphBuilder> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task BuildFromTaskAsync(TaskRecord task, ResolvedLinks links, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        // Create or update Task node
        await ExecuteCypherAsync(conn,
            "MERGE (t:Task {key: $key}) SET t.summary = $summary, t.issue_type = $issue_type",
            new Dictionary<string, object?>
            {
                ["key"] = task.Key,
                ["summary"] = task.Summary ?? "",
                ["issue_type"] = task.IssueType ?? ""
            }, ct);

        // Developer node + ASSIGNED edge
        if (task.Assignee is not null)
        {
            await ExecuteCypherAsync(conn,
                "MERGE (d:Developer {name: $name})",
                new Dictionary<string, object?> { ["name"] = task.Assignee }, ct);

            await ExecuteCypherAsync(conn,
                "MATCH (t:Task {key: $key}), (d:Developer {name: $name}) MERGE (t)-[:ASSIGNED]->(d)",
                new Dictionary<string, object?> { ["key"] = task.Key, ["name"] = task.Assignee }, ct);
        }

        // Reporter node + REPORTED edge
        if (task.Reporter is not null)
        {
            await ExecuteCypherAsync(conn,
                "MERGE (d:Developer {name: $name})",
                new Dictionary<string, object?> { ["name"] = task.Reporter }, ct);

            await ExecuteCypherAsync(conn,
                "MATCH (t:Task {key: $key}), (d:Developer {name: $name}) MERGE (t)-[:REPORTED]->(d)",
                new Dictionary<string, object?> { ["key"] = task.Key, ["name"] = task.Reporter }, ct);
        }

        // Component nodes + BELONGS_TO edges
        if (task.ComponentsJson is not null)
        {
            var components = JsonSerializer.Deserialize<List<string>>(task.ComponentsJson) ?? [];
            foreach (var comp in components)
            {
                await ExecuteCypherAsync(conn,
                    "MERGE (c:Component {name: $name})",
                    new Dictionary<string, object?> { ["name"] = comp }, ct);

                await ExecuteCypherAsync(conn,
                    "MATCH (t:Task {key: $key}), (c:Component {name: $name}) MERGE (t)-[:BELONGS_TO]->(c)",
                    new Dictionary<string, object?> { ["key"] = task.Key, ["name"] = comp }, ct);
            }
        }

        // Linked task edges
        foreach (var link in links.DirectLinks)
        {
            await ExecuteCypherAsync(conn,
                "MERGE (t2:Task {key: $target_key}) SET t2.summary = $summary, t2.issue_type = $issue_type",
                new Dictionary<string, object?>
                {
                    ["target_key"] = link.Key,
                    ["summary"] = link.Summary,
                    ["issue_type"] = link.IssueType
                }, ct);

            await ExecuteCypherAsync(conn,
                "MATCH (t1:Task {key: $source}), (t2:Task {key: $target}) MERGE (t1)-[:LINKED {type: $link_type}]->(t2)",
                new Dictionary<string, object?>
                {
                    ["source"] = task.Key,
                    ["target"] = link.Key,
                    ["link_type"] = link.LinkType
                }, ct);
        }

        _logger.LogDebug("Built graph for {Key}: {LinkCount} links", task.Key, links.DirectLinks.Count);
    }

    private static async Task ExecuteCypherAsync(NpgsqlConnection conn, string cypher,
        Dictionary<string, object?> parameters, CancellationToken ct)
    {
        var paramJson = JsonSerializer.Serialize(parameters);
        var sql = $"SELECT * FROM cypher('jira_graph', $$ {cypher} $$, '{paramJson}') AS (result agtype)";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
