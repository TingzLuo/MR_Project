namespace MRProject.Api.DTOs.Scg;

public class ScgDetailResponse
{
    public long Id { get; set; }

    public long DocumentId { get; set; }

    public List<long> DocumentIds { get; set; } = [];

    public string ScgName { get; set; } = string.Empty;

    public string DocumentName { get; set; } = string.Empty;

    public string DocumentNamesSummary { get; set; } = string.Empty;

    public ScgGraphDto ScgGraph { get; set; } = new();

    public bool IsConfirmed { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
