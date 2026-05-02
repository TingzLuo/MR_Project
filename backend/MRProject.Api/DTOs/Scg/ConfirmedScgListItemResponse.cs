namespace MRProject.Api.DTOs.Scg;

public class ConfirmedScgListItemResponse
{
    public long Id { get; set; }

    public string ScgName { get; set; } = string.Empty;

    public string DocumentNamesSummary { get; set; } = string.Empty;

    public DateTime? ConfirmedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
