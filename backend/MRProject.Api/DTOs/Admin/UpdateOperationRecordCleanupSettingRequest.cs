using System.ComponentModel.DataAnnotations;

namespace MRProject.Api.DTOs.Admin;

public class UpdateOperationRecordCleanupSettingRequest
{
    public bool Enabled { get; set; }

    [Range(1, 3650, ErrorMessage = "保留天数必须在1到3650之间")]
    public int RetentionDays { get; set; }

    [Range(1, 720, ErrorMessage = "清理间隔数值必须在1到720之间")]
    public int IntervalValue { get; set; }

    [Required(ErrorMessage = "清理间隔单位不能为空")]
    public string IntervalUnit { get; set; } = string.Empty;
}
