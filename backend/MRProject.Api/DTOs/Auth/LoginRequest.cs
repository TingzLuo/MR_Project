using System.ComponentModel.DataAnnotations;

namespace MRProject.Api.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "用户名或邮箱不能为空")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;
}
