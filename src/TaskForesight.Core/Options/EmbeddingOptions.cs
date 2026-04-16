namespace TaskForesight.Core.Options;

public class EmbeddingOptions
{
    public string Provider { get; set; } = "gemini";
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "text-embedding-004";
    public int Dimensions { get; set; } = 768;
}
