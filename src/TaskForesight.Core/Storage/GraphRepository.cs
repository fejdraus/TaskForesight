using System.Text.Json;
using Microsoft.Extensions.Logging;
using Npgsql;
using TaskForesight.Core.Interfaces;
using TaskForesight.Shared.Dto;

namespace TaskForesight.Core.Storage;

public class GraphRepository : IGraphRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<GraphRepository> _logger;

    public GraphRepository(NpgsqlDataSource dataSource, ILogger<GraphRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<GraphData> GetTaskGraphAsync(string key, int depth = 3, CancellationToken ct = default)
    {
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();

        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        var sql = $"""
            SELECT * FROM cypher('jira_graph', $$
                MATCH (t:Task {{key: '{EscapeCypher(key)}'}})-[r*1..{depth}]-(n)
                RETURN t, r, n
            $$) AS (source agtype, rels agtype, target agtype)
            """;

        try
        {
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            var seenNodes = new HashSet<string>();

            while (await reader.ReadAsync(ct))
            {
                var sourceStr = reader.GetString(0);
                var targetStr = reader.GetString(2);

                AddNodeFromAgtype(sourceStr, nodes, seenNodes);
                AddNodeFromAgtype(targetStr, nodes, seenNodes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query graph for {Key}", key);
        }

        return new GraphData(nodes, edges);
    }

    public async Task<IReadOnlyList<CrossComponentRisk>> GetCrossComponentRisksAsync(string component,
        CancellationToken ct = default)
    {
        var results = new List<CrossComponentRisk>();

        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        var sql = $"""
            SELECT * FROM cypher('jira_graph', $$
                MATCH (c1:Component {{name: '{EscapeCypher(component)}'}})<-[:BELONGS_TO]-(t:Task)-[:LINKED]->(t2:Task)-[:BELONGS_TO]->(c2:Component)
                WHERE c1 <> c2
                RETURN c2.name, count(t2)
            $$) AS (component_name agtype, cross_bugs agtype)
            """;

        try
        {
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var name = reader.GetString(0).Trim('"');
                var count = int.Parse(reader.GetString(1));
                results.Add(new CrossComponentRisk(name, count));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query cross-component risks for {Component}", component);
        }

        return results;
    }

    private static void AddNodeFromAgtype(string agtype, List<GraphNode> nodes, HashSet<string> seen)
    {
        try
        {
            var json = JsonDocument.Parse(agtype.TrimEnd("::vertex".ToCharArray()));
            var root = json.RootElement;

            var id = root.GetProperty("id").ToString();
            if (!seen.Add(id)) return;

            var label = root.GetProperty("label").GetString() ?? "";
            var props = root.GetProperty("properties");
            var propsDict = new Dictionary<string, object>();

            foreach (var prop in props.EnumerateObject())
                propsDict[prop.Name] = prop.Value.ToString();

            var nodeId = propsDict.TryGetValue("key", out var k) ? k.ToString()!
                : propsDict.TryGetValue("name", out var n) ? n.ToString()!
                : id;

            nodes.Add(new GraphNode(nodeId, propsDict.GetValueOrDefault("summary")?.ToString() ?? nodeId, label, propsDict));
        }
        catch
        {
            // Skip unparseable agtype values
        }
    }

    private static string EscapeCypher(string value) => value.Replace("'", "\\'");
}
