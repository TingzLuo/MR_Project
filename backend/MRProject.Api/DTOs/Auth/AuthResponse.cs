namespace MRProject.Api.DTOs.Auth;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;

    public CurrentUserResponse UserInfo { get; set; } = new();
}
