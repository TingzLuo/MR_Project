using MRProject.Api.Common;
using MRProject.Api.DTOs.Admin;

namespace MRProject.Api.Services.Interfaces;

public interface IAdminService
{
    Task<AdminOverviewResponse> GetOverviewAsync();

    Task<PagedResult<AdminUserListItemResponse>> GetUserPagedListAsync(AdminUserQueryRequest request);

    Task<AdminUserListItemResponse> CreateUserAsync(AdminSaveUserRequest request);

    Task<AdminUserListItemResponse> UpdateUserAsync(long userId, AdminUpdateUserRequest request);

    Task DeleteUserAsync(long userId);

    Task BatchDeleteUsersAsync(List<long> userIds);

    Task<OperationRecordCleanupSettingResponse> GetOperationRecordCleanupSettingAsync();

    Task<OperationRecordCleanupSettingResponse> UpdateOperationRecordCleanupSettingAsync(UpdateOperationRecordCleanupSettingRequest request);
}
