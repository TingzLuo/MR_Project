using System.ComponentModel.DataAnnotations;

namespace MRProject.Api.DTOs.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 4, ErrorMessage = "用户名长度必须在4到50之间")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(50, MinimumLength = 6, ErrorMessage = "密码长度必须在6到50之间")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "确认密码不能为空")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(50, ErrorMessage = "姓名长度不能超过50")]
    public string RealName { get; set; } = string.Empty;

    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [StringLength(100, ErrorMessage = "邮箱长度不能超过100")]
    public string Email { get; set; } = string.Empty;
}
