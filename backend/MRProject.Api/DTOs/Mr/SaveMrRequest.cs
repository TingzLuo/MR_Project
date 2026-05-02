using System.ComponentModel.DataAnnotations;

namespace MRProject.Api.DTOs.Mr;

public class SaveMrRequest
{
    [Required(ErrorMessage = "MR 列表不能为空")]
    public List<MrItemDto> MrItems { get; set; } = [];
}
