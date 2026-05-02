using System.ComponentModel.DataAnnotations;

namespace MRProject.Api.DTOs.Users;

public class UpdatePasswordRequest
{
    [Required(ErrorMessage = "旧密码不能为空")]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "新密码不能为空")]
    [StringLength(50, MinimumLength = 6, ErrorMessage = "新密码长度必须在6到50之间")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "确认密码不能为空")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
