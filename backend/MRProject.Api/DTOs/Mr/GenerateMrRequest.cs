using System.ComponentModel.DataAnnotations;

namespace MRProject.Api.DTOs.Mr;

public class GenerateMrRequest
{
    [Required(ErrorMessage = "SCG ID不能为空")]
    public long ScgId { get; set; }
}
