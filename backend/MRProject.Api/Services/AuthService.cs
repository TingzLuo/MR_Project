using MRProject.Api.Common;
using MRProject.Api.Data;
using MRProject.Api.DTOs.Auth;
using MRProject.Api.DTOs.Users;
using MRProject.Api.Entities;
using MRProject.Api.Enums;
using MRProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MRProject.Api.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtOptions _jwtOptions;

    public AuthService(ApplicationDbContext dbContext, IPasswordHasher<User> passwordHasher, IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
        {
            throw new AppException("两次输入的密码不一致");
        }

        var username = request.Username.Trim();
        var email = request.Email.Trim();
        var realName = request.RealName.Trim();

        var usernameExists = await _dbContext.Users.AnyAsync(user => user.Username == username && !user.IsDeleted);
        if (usernameExists) throw new AppException("用户名已存在", 409);

        var emailExists = await _dbContext.Users.AnyAsync(user => user.Email == email && !user.IsDeleted);
        if (emailExists) throw new AppException("邮箱已存在", 409);

        var now = SystemTime.Now();
        var user = new User
        {
            Username = username,
            RealName = realName,
            Email = email,
            Phone = string.Empty,
            ProfileDescription = string.Empty,
            Role = UserRole.User,
            Status = UserStatus.Enabled,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var account = request.Username.Trim();
        var user = await _dbContext.Users.FirstOrDefaultAsync(item =>
            !item.IsDeleted && (item.Username == account || item.Email == account));
        if (user is null) throw new AppException("用户名/邮箱或密码错误", 400);
        if (user.Status != UserStatus.Enabled) throw new AppException("当前账号已被禁用", 403);

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed) throw new AppException("用户名/邮箱或密码错误", 400);

        user.LastLoginAt = SystemTime.Now();
        user.UpdatedAt = SystemTime.Now();
        await _dbContext.SaveChangesAsync();

        return new AuthResponse { Token = GenerateToken(user), UserInfo = MapCurrentUser(user) };
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(long userId)
    {
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId && !item.IsDeleted);
        if (user is null) throw new AppException("用户不存在", 404);
        if (user.Status != UserStatus.Enabled) throw new AppException("当前账号已被禁用", 403);
        return MapCurrentUser(user);
    }

    private string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static CurrentUserResponse MapCurrentUser(User user)
    {
        return new CurrentUserResponse
        {
            Id = user.Id,
            Username = user.Username,
            RealName = user.RealName,
            Email = user.Email,
            Phone = user.Phone,
            ProfileDescription = user.ProfileDescription,
            Role = user.Role == UserRole.Admin ? "admin" : "user"
        };
    }
}
