using System.ComponentModel.DataAnnotations;

namespace MRProject.Api.DTOs.Scg;

public class GenerateScgRequest
{
    [Required(ErrorMessage = "文件ID列表不能为空")]
    [MinLength(1, ErrorMessage = "至少选择一个文件")]
    public List<long> DocumentIds { get; set; } = [];
}
