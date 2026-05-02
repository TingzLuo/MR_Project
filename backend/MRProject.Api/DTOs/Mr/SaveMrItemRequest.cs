using System.ComponentModel.DataAnnotations;

namespace MRProject.Api.DTOs.Mr;

public class SaveMrItemRequest
{
    [Required(ErrorMessage = "MR ID不能为空")]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "输入关系不能为空")]
    public string InputRelation { get; set; } = string.Empty;

    [Required(ErrorMessage = "输出关系不能为空")]
    public string OutputRelation { get; set; } = string.Empty;

    [Required(ErrorMessage = "描述不能为空")]
    public string Description { get; set; } = string.Empty;
}
