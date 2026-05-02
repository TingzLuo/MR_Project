namespace MRProject.Api.Entities;

public class ScgRecord : BaseEntity
{
    public long DocumentId { get; set; }

    public long UserId { get; set; }

    public string ScgName { get; set; } = string.Empty;

    public string DocumentIdsKey { get; set; } = string.Empty;

    public string DocumentNamesSummary { get; set; } = string.Empty;

    public string ScgJson { get; set; } = string.Empty;

    public string SourceTextSnapshot { get; set; } = string.Empty;

    public bool IsConfirmed { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public bool IsDeleted { get; set; }

    public Document? Document { get; set; }

    public User? User { get; set; }
}
