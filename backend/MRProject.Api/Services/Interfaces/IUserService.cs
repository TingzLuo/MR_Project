using MRProject.Api.DTOs.Users;
using MRProject.Api.DTOs.Admin;

namespace MRProject.Api.Services.Interfaces;

public interface IUserService
{
    Task<ProfileResponse> GetProfileAsync(long userId);

    Task<ProfileResponse> UpdateProfileAsync(long userId, UpdateProfileRequest request);

    Task UpdatePasswordAsync(long userId, UpdatePasswordRequest request);

    Task<UserOperationCleanupSettingResponse> GetOperationRecordCleanupSettingAsync(long userId);

    Task<UserOperationCleanupSettingResponse> UpdateOperationRecordCleanupSettingAsync(long userId, UpdateUserOperationCleanupSettingRequest request);
}
