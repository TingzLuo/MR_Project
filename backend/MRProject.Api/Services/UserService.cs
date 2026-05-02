using MRProject.Api.Common;
using MRProject.Api.Data;
using MRProject.Api.DTOs.Users;
using MRProject.Api.DTOs.Admin;
using MRProject.Api.Entities;
using MRProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MRProject.Api.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly OperationRecordCleanupOptions _cleanupOptions;

    public UserService(ApplicationDbContext dbContext, IPasswordHasher<User> passwordHasher, IOptions<OperationRecordCleanupOptions> cleanupOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _cleanupOptions = cleanupOptions.Value;
    }

    public async Task<ProfileResponse> GetProfileAsync(long userId)
    {
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId && !item.IsDeleted);
        if (user is null) throw new AppException("用户不存在", 404);
        return MapProfile(user);
    }

    public async Task<ProfileResponse> UpdateProfileAsync(long userId, UpdateProfileRequest request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(item => item.Id == userId && !item.IsDeleted);
        if (user is null) throw new AppException("用户不存在", 404);

        var username = request.Username.Trim();
        var email = request.Email.Trim();
        var usernameExists = await _dbContext.Users.AnyAsync(item => item.Id != userId && item.Username == username && !item.IsDeleted);
        if (usernameExists) throw new AppException("用户名已存在", 409);
        var emailExists = await _dbContext.Users.AnyAsync(item => item.Id != userId && item.Email == email && !item.IsDeleted);
        if (emailExists) throw new AppException("邮箱已存在", 409);

        user.Username = username;
        user.RealName = request.RealName.Trim();
        user.Email = email;
        user.Phone = request.Phone.Trim();
        user.ProfileDescription = request.ProfileDescription.Trim();
        user.UpdatedAt = SystemTime.Now();
        await _dbContext.SaveChangesAsync();
        await AddUserOperationRecordAsync(userId, "updateProfile", "个人信息", JsonSerializer.Serialize(new
        {
            user.Username,
            user.RealName,
            user.Email,
            user.Phone,
            user.ProfileDescription
        }));

        return MapProfile(user);
    }

    public async Task UpdatePasswordAsync(long userId, UpdatePasswordRequest request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(item => item.Id == userId && !item.IsDeleted);
        if (user is null) throw new AppException("用户不存在", 404);
        if (request.NewPassword != request.ConfirmPassword) throw new AppException("两次输入的新密码不一致");
        if (request.OldPassword == request.NewPassword) throw new AppException("新密码不能与旧密码一致", 400);

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.OldPassword);
        if (verifyResult == PasswordVerificationResult.Failed) throw new AppException("旧密码错误", 400);

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = SystemTime.Now();
        await _dbContext.SaveChangesAsync();
        await AddUserOperationRecordAsync(userId, "updatePassword", "密码", "password updated");
    }

    public async Task<UserOperationCleanupSettingResponse> GetOperationRecordCleanupSettingAsync(long userId)
    {
        var globalSetting = await _dbContext.OperationRecordCleanupSettings.AsNoTracking().OrderBy(item => item.Id).FirstOrDefaultAsync();
        var userSetting = await _dbContext.UserOperationCleanupSettings.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == userId);

        var globalEnabled = globalSetting?.Enabled ?? _cleanupOptions.Enabled;
        var globalRetentionDays = globalSetting?.RetentionDays ?? _cleanupOptions.RetentionDays;
        var globalIntervalValue = globalSetting?.IntervalValue ?? _cleanupOptions.IntervalValue;
        var globalIntervalUnit = NormalizeIntervalUnit(globalSetting?.IntervalUnit ?? _cleanupOptions.IntervalUnit);

        if (userSetting is null)
        {
            return new UserOperationCleanupSettingResponse
            {
                UseGlobalSetting = true,
                Enabled = globalEnabled,
                RetentionDays = globalRetentionDays,
                IntervalValue = globalIntervalValue,
                IntervalUnit = globalIntervalUnit,
                GlobalEnabled = globalEnabled,
                GlobalRetentionDays = globalRetentionDays,
                GlobalIntervalValue = globalIntervalValue,
                GlobalIntervalUnit = globalIntervalUnit
            };
        }

        return new UserOperationCleanupSettingResponse
        {
            UseGlobalSetting = false,
            Enabled = userSetting.Enabled,
            RetentionDays = userSetting.RetentionDays,
            IntervalValue = userSetting.IntervalValue,
            IntervalUnit = userSetting.IntervalUnit,
            GlobalEnabled = globalEnabled,
            GlobalRetentionDays = globalRetentionDays,
            GlobalIntervalValue = globalIntervalValue,
            GlobalIntervalUnit = globalIntervalUnit
        };
    }

    public async Task<UserOperationCleanupSettingResponse> UpdateOperationRecordCleanupSettingAsync(long userId, UpdateUserOperationCleanupSettingRequest request)
    {
        var userSetting = await _dbContext.UserOperationCleanupSettings.FirstOrDefaultAsync(item => item.UserId == userId);
        if (request.UseGlobalSetting)
        {
            if (userSetting is not null)
            {
                _dbContext.UserOperationCleanupSettings.Remove(userSetting);
                await _dbContext.SaveChangesAsync();
            }

            return await GetOperationRecordCleanupSettingAsync(userId);
        }

        var normalizedUnit = NormalizeIntervalUnit(request.IntervalUnit);
        if (userSetting is null)
        {
            userSetting = new UserOperationCleanupSetting
            {
                UserId = userId,
                Enabled = request.Enabled,
                RetentionDays = request.RetentionDays,
                IntervalValue = request.IntervalValue,
                IntervalUnit = normalizedUnit,
                CreatedAt = SystemTime.Now(),
                UpdatedAt = SystemTime.Now()
            };
            _dbContext.UserOperationCleanupSettings.Add(userSetting);
        }
        else
        {
            userSetting.Enabled = request.Enabled;
            userSetting.RetentionDays = request.RetentionDays;
            userSetting.IntervalValue = request.IntervalValue;
            userSetting.IntervalUnit = normalizedUnit;
            userSetting.UpdatedAt = SystemTime.Now();
        }

        await _dbContext.SaveChangesAsync();
        return await GetOperationRecordCleanupSettingAsync(userId);
    }

    private async Task AddUserOperationRecordAsync(long userId, string operationType, string operationTarget, string operationSnapshot)
    {
        _dbContext.UserOperationRecords.Add(new UserOperationRecord
        {
            UserId = userId,
            OperationType = operationType,
            OperationTarget = operationTarget,
            OperationSnapshot = operationSnapshot,
            CreatedAt = SystemTime.Now(),
            UpdatedAt = SystemTime.Now()
        });
        await _dbContext.SaveChangesAsync();
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

    private static ProfileResponse MapProfile(User user)
    {
        return new ProfileResponse
        {
            Id = user.Id,
            Username = user.Username,
            RealName = user.RealName,
            Email = user.Email,
            Phone = user.Phone,
            ProfileDescription = user.ProfileDescription,
            Role = user.Role == Enums.UserRole.Admin ? "admin" : "user",
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };
    }
}
