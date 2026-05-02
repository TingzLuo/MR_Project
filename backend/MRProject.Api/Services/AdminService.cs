using MRProject.Api.Common;
using MRProject.Api.Data;
using MRProject.Api.DTOs.Admin;
using MRProject.Api.Entities;
using MRProject.Api.Enums;
using MRProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MRProject.Api.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly OperationRecordCleanupOptions _cleanupOptions;

    public AdminService(ApplicationDbContext dbContext, IPasswordHasher<User> passwordHasher, IOptions<OperationRecordCleanupOptions> cleanupOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _cleanupOptions = cleanupOptions.Value;
    }

    public async Task<AdminOverviewResponse> GetOverviewAsync()
    {
        return new AdminOverviewResponse
        {
            UserCount = await _dbContext.Users.CountAsync(item => !item.IsDeleted),
            DocumentCount = await _dbContext.Documents.CountAsync(item => !item.IsDeleted),
            ScgCount = await _dbContext.ScgRecords.CountAsync(item => !item.IsDeleted),
            MrCount = await _dbContext.MrRecords.CountAsync(item => !item.IsDeleted)
        };
    }

    public async Task<PagedResult<AdminUserListItemResponse>> GetUserPagedListAsync(AdminUserQueryRequest request)
    {
        var safePageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var safePageSize = request.PageSize <= 0 ? 10 : request.PageSize;
        var keyword = request.Keyword?.Trim() ?? string.Empty;

        var query = _dbContext.Users.AsNoTracking().Where(item => !item.IsDeleted);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(item => item.Username.Contains(keyword) || item.RealName.Contains(keyword) || item.Email.Contains(keyword) || item.Phone.Contains(keyword));
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(item => item.CreatedAt)
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .Select(item => MapItem(item))
            .ToListAsync();

        return new PagedResult<AdminUserListItemResponse>
        {
            Items = items,
            Total = total,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<AdminUserListItemResponse> CreateUserAsync(AdminSaveUserRequest request)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();
        if (await _dbContext.Users.AnyAsync(item => item.Username == username && !item.IsDeleted)) throw new AppException("用户名已存在", 409);
        if (await _dbContext.Users.AnyAsync(item => item.Email == email && !item.IsDeleted)) throw new AppException("邮箱已存在", 409);

        var role = ParseRole(request.Role);
        var status = ParseStatus(request.Status);
        var now = SystemTime.Now();
        var user = new User
        {
            Username = username,
            RealName = request.RealName.Trim(),
            Email = email,
            Phone = request.Phone.Trim(),
            ProfileDescription = request.ProfileDescription.Trim(),
            Role = role,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return MapItem(user);
    }

    public async Task<AdminUserListItemResponse> UpdateUserAsync(long userId, AdminUpdateUserRequest request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(item => item.Id == userId && !item.IsDeleted);
        if (user is null) throw new AppException("用户不存在", 404);

        var username = request.Username.Trim();
        var email = request.Email.Trim();
        if (await _dbContext.Users.AnyAsync(item => item.Id != userId && item.Username == username && !item.IsDeleted)) throw new AppException("用户名已存在", 409);
        if (await _dbContext.Users.AnyAsync(item => item.Id != userId && item.Email == email && !item.IsDeleted)) throw new AppException("邮箱已存在", 409);

        user.Username = username;
        user.RealName = request.RealName.Trim();
        user.Email = email;
        user.Phone = request.Phone.Trim();
        user.ProfileDescription = request.ProfileDescription.Trim();
        user.Role = ParseRole(request.Role);
        user.Status = ParseStatus(request.Status);
        user.UpdatedAt = SystemTime.Now();
        await _dbContext.SaveChangesAsync();
        return MapItem(user);
    }

    public async Task DeleteUserAsync(long userId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(item => item.Id == userId && !item.IsDeleted);
        if (user is null) throw new AppException("用户不存在", 404);
        user.IsDeleted = true;
        user.UpdatedAt = SystemTime.Now();
        await _dbContext.SaveChangesAsync();
    }

    public async Task BatchDeleteUsersAsync(List<long> userIds)
    {
        var ids = userIds.Where(id => id > 0).Distinct().ToList();
        var users = await _dbContext.Users.Where(item => ids.Contains(item.Id) && !item.IsDeleted).ToListAsync();
        foreach (var user in users)
        {
            user.IsDeleted = true;
            user.UpdatedAt = SystemTime.Now();
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task<OperationRecordCleanupSettingResponse> GetOperationRecordCleanupSettingAsync()
    {
        var setting = await _dbContext.OperationRecordCleanupSettings.AsNoTracking().OrderBy(item => item.Id).FirstOrDefaultAsync();
        if (setting is null)
        {
            return new OperationRecordCleanupSettingResponse
            {
                Enabled = _cleanupOptions.Enabled,
                RetentionDays = _cleanupOptions.RetentionDays,
                IntervalValue = _cleanupOptions.IntervalValue,
                IntervalUnit = NormalizeIntervalUnit(_cleanupOptions.IntervalUnit)
            };
        }

        return new OperationRecordCleanupSettingResponse
        {
            Enabled = setting.Enabled,
            RetentionDays = setting.RetentionDays,
            IntervalValue = setting.IntervalValue,
            IntervalUnit = setting.IntervalUnit
        };
    }

    public async Task<OperationRecordCleanupSettingResponse> UpdateOperationRecordCleanupSettingAsync(UpdateOperationRecordCleanupSettingRequest request)
    {
        var setting = await _dbContext.OperationRecordCleanupSettings.OrderBy(item => item.Id).FirstOrDefaultAsync();
        if (setting is null)
        {
            setting = new OperationRecordCleanupSetting
            {
                Enabled = request.Enabled,
                RetentionDays = request.RetentionDays,
                IntervalValue = request.IntervalValue,
                IntervalUnit = NormalizeIntervalUnit(request.IntervalUnit),
                CreatedAt = SystemTime.Now(),
                UpdatedAt = SystemTime.Now()
            };
            _dbContext.OperationRecordCleanupSettings.Add(setting);
        }
        else
        {
            setting.Enabled = request.Enabled;
            setting.RetentionDays = request.RetentionDays;
            setting.IntervalValue = request.IntervalValue;
            setting.IntervalUnit = NormalizeIntervalUnit(request.IntervalUnit);
            setting.UpdatedAt = SystemTime.Now();
        }

        await _dbContext.SaveChangesAsync();
        return new OperationRecordCleanupSettingResponse
        {
            Enabled = setting.Enabled,
            RetentionDays = setting.RetentionDays,
            IntervalValue = setting.IntervalValue,
            IntervalUnit = setting.IntervalUnit
        };
    }

    private static string NormalizeIntervalUnit(string intervalUnit)
    {
        return intervalUnit?.Trim().ToLowerInvariant() switch
        {
            "day" => "day",
            "week" => "week",
            "month" => "month",
            _ => "hour"
        };
    }

    private static AdminUserListItemResponse MapItem(User user)
    {
        return new AdminUserListItemResponse
        {
            Id = user.Id,
            Username = user.Username,
            RealName = user.RealName,
            Email = user.Email,
            Phone = string.IsNullOrWhiteSpace(user.Phone) ? "未绑定" : user.Phone,
            Role = user.Role == UserRole.Admin ? "管理员" : "普通用户",
            Status = user.Status == UserStatus.Enabled ? "正常用户" : "禁用用户",
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
    }

    private static UserRole ParseRole(string role)
    {
        return role switch
        {
            "admin" or "管理员" => UserRole.Admin,
            _ => UserRole.User
        };
    }

    private static UserStatus ParseStatus(string status)
    {
        return status switch
        {
            "disabled" or "禁用用户" => UserStatus.Disabled,
            _ => UserStatus.Enabled
        };
    }
}
