namespace TaskForesight.Core.Options;

public class EmbeddingOptions
{
    public string Provider { get; set; } = "ollama";
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "nomic-embed-text";
    public int Dimensions { get; set; } = 768;
}
