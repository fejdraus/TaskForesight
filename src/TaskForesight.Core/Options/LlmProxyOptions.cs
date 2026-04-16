namespace TaskForesight.Core.Options;

public class LlmProxyOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8080/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.5-pro";
}
