using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskForesight.Core.Interfaces;
using TaskForesight.Core.Models;
using TaskForesight.Core.Options;

namespace TaskForesight.Core.Collector;

public class JiraClient : IJiraClient
{
    private readonly HttpClient _http;
    private readonly JiraOptions _options;
    private readonly ILogger<JiraClient> _logger;

    private const string Fields =
        "summary,description,status,assignee,reporter,issuetype,priority," +
        "issuelinks,components,labels,created,resolutiondate," +
        "timeoriginalestimate,timespent," +
        "aggregatetimeoriginalestimate,aggregatetimespent";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public JiraClient(HttpClient http, IOptions<JiraOptions> options, ILogger<JiraClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<JiraIssue?> GetIssueAsync(string key, bool expandChangelog = true, CancellationToken ct = default)
    {
        var expand = expandChangelog ? "&expand=changelog" : "";
        var url = $"/rest/api/2/issue/{key}?fields={Fields}{expand}";

        _logger.LogDebug("GET {Url}", url);

        try
        {
            var response = await _http.GetAsync(url, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Issue {Key} not found", key);
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<JiraIssue>(JsonOptions, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogError("Timeout fetching issue {Key}", key);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching issue {Key}", key);
            return null;
        }
    }

    public async Task<JiraSearchResult> SearchAsync(string jql, int startAt = 0, int maxResults = 50,
        bool expandChangelog = true, CancellationToken ct = default)
    {
        var expand = expandChangelog ? "&expand=changelog" : "";
        var encodedJql = Uri.EscapeDataString(jql);
        var url = $"/rest/api/2/search?jql={encodedJql}&startAt={startAt}&maxResults={maxResults}&fields={Fields}{expand}";

        _logger.LogDebug("Search: startAt={StartAt}, maxResults={MaxResults}, jql={Jql}", startAt, maxResults, jql);

        try
        {
            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<JiraSearchResult>(JsonOptions, ct)
                   ?? new JiraSearchResult();
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogError("Timeout on search: {Jql}", jql);
            return new JiraSearchResult();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error on search: {Jql}", jql);
            return new JiraSearchResult();
        }
    }

    public async Task<IReadOnlyList<JiraComment>> GetCommentsAsync(string key, int maxComments = 10,
        CancellationToken ct = default)
    {
        var url = $"/rest/api/2/issue/{key}/comment";

        _logger.LogDebug("GET comments for {Key}", key);

        try
        {
            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JiraCommentResult>(JsonOptions, ct);
            var comments = result?.Comments ?? [];
            return comments.Count > maxComments
                ? comments.Skip(comments.Count - maxComments).ToList()
                : comments;
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException)
        {
            _logger.LogError(ex, "Error fetching comments for {Key}", key);
            return [];
        }
    }
}
