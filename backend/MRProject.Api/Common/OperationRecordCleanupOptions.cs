namespace MRProject.Api.Common;

public class OperationRecordCleanupOptions
{
    public bool Enabled { get; set; } = true;

    public int RetentionDays { get; set; } = 90;

    public int IntervalValue { get; set; } = 24;

    public string IntervalUnit { get; set; } = "hour";
}
