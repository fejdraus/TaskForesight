using System.Text.Json.Serialization;

namespace TaskForesight.Core.Models;

public class JiraSearchResult
{
    [JsonPropertyName("startAt")]
    public int StartAt { get; set; }

    [JsonPropertyName("maxResults")]
    public int MaxResults { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("issues")]
    public List<JiraIssue> Issues { get; set; } = [];
}

public class JiraIssue
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public JiraFields Fields { get; set; } = new();

    [JsonPropertyName("changelog")]
    public JiraChangelog? Changelog { get; set; }
}

public class JiraFields
{
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("status")]
    public JiraStatus? Status { get; set; }

    [JsonPropertyName("assignee")]
    public JiraUser? Assignee { get; set; }

    [JsonPropertyName("reporter")]
    public JiraUser? Reporter { get; set; }

    [JsonPropertyName("issuetype")]
    public JiraIssueType? IssueType { get; set; }

    [JsonPropertyName("priority")]
    public JiraPriority? Priority { get; set; }

    [JsonPropertyName("issuelinks")]
    public List<JiraIssueLink> IssueLinks { get; set; } = [];

    [JsonPropertyName("components")]
    public List<JiraComponent> Components { get; set; } = [];

    [JsonPropertyName("labels")]
    public List<string> Labels { get; set; } = [];

    [JsonPropertyName("created")]
    public string? Created { get; set; }

    [JsonPropertyName("resolutiondate")]
    public string? ResolutionDate { get; set; }

    /// <summary>Original estimate in seconds.</summary>
    [JsonPropertyName("timeoriginalestimate")]
    public long? TimeOriginalEstimate { get; set; }

    /// <summary>Time spent in seconds.</summary>
    [JsonPropertyName("timespent")]
    public long? TimeSpent { get; set; }

    /// <summary>Aggregate original estimate in seconds (includes subtasks).</summary>
    [JsonPropertyName("aggregatetimeoriginalestimate")]
    public long? AggregateTimeOriginalEstimate { get; set; }

    /// <summary>Aggregate time spent in seconds (includes subtasks).</summary>
    [JsonPropertyName("aggregatetimespent")]
    public long? AggregateTimeSpent { get; set; }
}

public class JiraStatus
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("statusCategory")]
    public JiraStatusCategory? StatusCategory { get; set; }
}

public class JiraStatusCategory
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class JiraUser
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }
}

public class JiraIssueType
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("subtask")]
    public bool Subtask { get; set; }
}

public class JiraPriority
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class JiraComponent
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class JiraIssueLink
{
    [JsonPropertyName("type")]
    public JiraLinkType Type { get; set; } = new();

    [JsonPropertyName("outwardIssue")]
    public JiraLinkedIssue? OutwardIssue { get; set; }

    [JsonPropertyName("inwardIssue")]
    public JiraLinkedIssue? InwardIssue { get; set; }
}

public class JiraLinkType
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("inward")]
    public string Inward { get; set; } = string.Empty;

    [JsonPropertyName("outward")]
    public string Outward { get; set; } = string.Empty;
}

public class JiraLinkedIssue
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public JiraLinkedIssueFields Fields { get; set; } = new();
}

public class JiraLinkedIssueFields
{
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("status")]
    public JiraStatus? Status { get; set; }

    [JsonPropertyName("issuetype")]
    public JiraIssueType? IssueType { get; set; }
}

public class JiraChangelog
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("maxResults")]
    public int MaxResults { get; set; }

    [JsonPropertyName("histories")]
    public List<JiraHistory> Histories { get; set; } = [];
}

public class JiraHistory
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public JiraUser Author { get; set; } = new();

    [JsonPropertyName("created")]
    public string Created { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<JiraHistoryItem> Items { get; set; } = [];
}

public class JiraHistoryItem
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("fieldtype")]
    public string? FieldType { get; set; }

    [JsonPropertyName("fromString")]
    public string? FromString { get; set; }

    [JsonPropertyName("toString")]
    public new string? ToString { get; set; }
}

public class JiraCommentResult
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("comments")]
    public List<JiraComment> Comments { get; set; } = [];
}

public class JiraComment
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public JiraUser Author { get; set; } = new();

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public string Created { get; set; } = string.Empty;

    [JsonPropertyName("updated")]
    public string? Updated { get; set; }
}
