using MRProject.Api.Common;
using MRProject.Api.DTOs.Auth;
using MRProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MRProject.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IAuthService authService, ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(item => item.Errors).FirstOrDefault()?.ErrorMessage ?? "请求参数错误";
            return Ok(ApiResponse<object>.Fail(400, firstError));
        }

        await _authService.RegisterAsync(request);
        return Ok(ApiResponse<object>.Success(null, "注册成功"));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(item => item.Errors).FirstOrDefault()?.ErrorMessage ?? "请求参数错误";
            return Ok(ApiResponse<AuthResponse>.Fail(400, firstError));
        }

        var response = await _authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponse>.Success(response));
    }

    [HttpGet("current-user")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> GetCurrentUser()
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _authService.GetCurrentUserAsync(currentUser.UserId);
        return Ok(ApiResponse<CurrentUserResponse>.Success(response));
    }
}
