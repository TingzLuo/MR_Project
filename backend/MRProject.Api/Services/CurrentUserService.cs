using MRProject.Api.Common;
using MRProject.Api.Enums;
using MRProject.Api.Services.Interfaces;
using System.Security.Claims;

namespace MRProject.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUser GetCurrentUser()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            throw new AppException("未登录或 token 无效", 401);
        }

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var usernameClaim = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        var roleClaim = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;

        if (!long.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(usernameClaim) || string.IsNullOrWhiteSpace(roleClaim))
        {
            throw new AppException("当前用户信息解析失败", 401);
        }

        if (!Enum.TryParse<UserRole>(roleClaim, true, out var role))
        {
            throw new AppException("当前用户角色解析失败", 401);
        }

        return new CurrentUser
        {
            UserId = userId,
            Username = usernameClaim,
            Role = role
        };
    }
}
