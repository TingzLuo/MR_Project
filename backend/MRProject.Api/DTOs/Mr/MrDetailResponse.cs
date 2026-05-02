namespace MRProject.Api.DTOs.Mr;

public class MrDetailResponse
{
    public long Id { get; set; }

    public long ScgId { get; set; }

    public string DocumentNamesSummary { get; set; } = string.Empty;

    public List<MrItemDto> MrItems { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
