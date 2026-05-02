namespace MRProject.Api.Entities;

public class LlmCallLog : BaseEntity
{
    public string BusinessType { get; set; } = string.Empty;

    public long BusinessId { get; set; }

    public string PromptContent { get; set; } = string.Empty;

    public string ResponseContent { get; set; } = string.Empty;

    public string CallStatus { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;
}
