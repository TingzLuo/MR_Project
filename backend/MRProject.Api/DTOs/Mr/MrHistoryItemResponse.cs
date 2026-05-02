namespace MRProject.Api.DTOs.Mr;

public class MrHistoryItemResponse
{
    public long Id { get; set; }

    public string OperationType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
