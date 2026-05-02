namespace MRProject.Api.Entities;

public class ScgHistoryRecord : BaseEntity
{
    public long ScgRecordId { get; set; }

    public long UserId { get; set; }

    public string ScgName { get; set; } = string.Empty;

    public string OperationType { get; set; } = string.Empty;

    public string ScgJson { get; set; } = string.Empty;

    public ScgRecord? ScgRecord { get; set; }

    public User? User { get; set; }
}
