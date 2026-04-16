namespace TaskForesight.Shared.Dto;

public record GraphData(
    IReadOnlyList<GraphNode> Nodes,
    IReadOnlyList<GraphEdge> Edges);

public record GraphNode(
    string Id,
    string Label,
    string Type,
    Dictionary<string, object>? Properties);

public record GraphEdge(
    string Source,
    string Target,
    string Type,
    Dictionary<string, object>? Properties);
