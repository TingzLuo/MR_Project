namespace MRProject.Api.Entities;

public class MrRecord : BaseEntity
{
    public long ScgRecordId { get; set; }

    public long UserId { get; set; }

    public string DocumentIdsKey { get; set; } = string.Empty;

    public string DocumentNamesSummary { get; set; } = string.Empty;

    public string MrJson { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public ScgRecord? ScgRecord { get; set; }

    public User? User { get; set; }
}
