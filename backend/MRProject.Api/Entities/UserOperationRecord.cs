namespace MRProject.Api.Entities;

public class UserOperationRecord : BaseEntity
{
    public long UserId { get; set; }

    public string OperationType { get; set; } = string.Empty;

    public string OperationTarget { get; set; } = string.Empty;

    public string OperationSnapshot { get; set; } = string.Empty;

    public User? User { get; set; }
}
