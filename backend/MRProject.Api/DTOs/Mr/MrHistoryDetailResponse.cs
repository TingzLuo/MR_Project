namespace MRProject.Api.DTOs.Mr;

public class MrHistoryDetailResponse
{
    public long Id { get; set; }

    public long MrRecordId { get; set; }

    public string OperationType { get; set; } = string.Empty;

    public List<MrItemDto> MrItems { get; set; } = [];

    public DateTime CreatedAt { get; set; }
}
