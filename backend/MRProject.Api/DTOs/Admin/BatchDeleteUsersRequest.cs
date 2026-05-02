using System.ComponentModel.DataAnnotations;

namespace MRProject.Api.DTOs.Admin;

public class BatchDeleteUsersRequest
{
    [Required(ErrorMessage = "用户ID列表不能为空")]
    [MinLength(1, ErrorMessage = "至少选择一个用户")]
    public List<long> UserIds { get; set; } = [];
}
