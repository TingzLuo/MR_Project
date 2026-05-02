namespace MRProject.Api.Entities;

public class MrHistoryRecord : BaseEntity
{
    public long MrRecordId { get; set; }

    public long UserId { get; set; }

    public string OperationType { get; set; } = string.Empty;

    public string MrJson { get; set; } = string.Empty;

    public MrRecord? MrRecord { get; set; }

    public User? User { get; set; }
}
