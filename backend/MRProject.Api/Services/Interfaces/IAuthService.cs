using MRProject.Api.DTOs.Auth;

namespace MRProject.Api.Services.Interfaces;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request);

    Task<AuthResponse> LoginAsync(LoginRequest request);

    Task<CurrentUserResponse> GetCurrentUserAsync(long userId);
}
