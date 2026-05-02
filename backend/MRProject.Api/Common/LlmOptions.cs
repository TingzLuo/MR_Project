namespace MRProject.Api.Common;

public class LlmOptions
{
    public bool UseMock { get; set; } = false;

    public string ModelName { get; set; } = "gpt-5.4";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 120;
}
