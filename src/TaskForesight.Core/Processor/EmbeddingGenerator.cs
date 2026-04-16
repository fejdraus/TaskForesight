using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskForesight.Core.Interfaces;
using TaskForesight.Core.Options;

namespace TaskForesight.Core.Processor;

public class EmbeddingGenerator : IEmbeddingGenerator
{
    private readonly HttpClient _http;
    private readonly EmbeddingOptions _options;
    private readonly ILogger<EmbeddingGenerator> _logger;

    public EmbeddingGenerator(HttpClient http, IOptions<EmbeddingOptions> options, ILogger<EmbeddingGenerator> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<float[]?> GenerateAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            var request = new OllamaEmbedRequest(_options.Model, text);
            var response = await _http.PostAsJsonAsync("/api/embed", request, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(ct: ct);
            if (result?.Embeddings is { Count: > 0 })
                return result.Embeddings[0];

            _logger.LogWarning("Empty embedding response for text: {Text}", text[..Math.Min(50, text.Length)]);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding");
            return null;
        }
    }

    private record OllamaEmbedRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input);

    private class OllamaEmbedResponse
    {
        [JsonPropertyName("embeddings")]
        public List<float[]>? Embeddings { get; set; }
    }
}
