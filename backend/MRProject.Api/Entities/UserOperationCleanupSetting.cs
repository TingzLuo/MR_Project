namespace MRProject.Api.Entities;

public class UserOperationCleanupSetting : BaseEntity
{
    public long UserId { get; set; }

    public bool Enabled { get; set; }

    public int RetentionDays { get; set; }

    public int IntervalValue { get; set; }

    public string IntervalUnit { get; set; } = string.Empty;

    public DateTime? LastCleanupAt { get; set; }

    public User? User { get; set; }
}
