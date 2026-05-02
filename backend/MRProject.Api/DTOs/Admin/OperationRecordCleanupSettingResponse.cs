namespace MRProject.Api.DTOs.Admin;

public class OperationRecordCleanupSettingResponse
{
    public bool Enabled { get; set; }

    public int RetentionDays { get; set; }

    public int IntervalValue { get; set; }

    public string IntervalUnit { get; set; } = string.Empty;
}
