namespace MRProject.Api.DTOs.Admin;

public class UserOperationCleanupSettingResponse
{
    public bool UseGlobalSetting { get; set; }

    public bool Enabled { get; set; }

    public int RetentionDays { get; set; }

    public int IntervalValue { get; set; }

    public string IntervalUnit { get; set; } = string.Empty;

    public bool GlobalEnabled { get; set; }

    public int GlobalRetentionDays { get; set; }

    public int GlobalIntervalValue { get; set; }

    public string GlobalIntervalUnit { get; set; } = string.Empty;
}
