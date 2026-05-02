using MRProject.Api.Common;
using MRProject.Api.DTOs.Admin;
using MRProject.Api.Enums;
using MRProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MRProject.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IExportService _exportService;

    public AdminController(IAdminService adminService, IExportService exportService)
    {
        _adminService = adminService;
        _exportService = exportService;
    }

    [HttpGet("dashboard/overview")]
    public async Task<ActionResult<ApiResponse<AdminOverviewResponse>>> GetOverview()
    {
        var response = await _adminService.GetOverviewAsync();
        return Ok(ApiResponse<AdminOverviewResponse>.Success(response));
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminUserListItemResponse>>>> GetUsers([FromQuery] AdminUserQueryRequest request)
    {
        var response = await _adminService.GetUserPagedListAsync(request);
        return Ok(ApiResponse<PagedResult<AdminUserListItemResponse>>.Success(response));
    }

    [HttpPost("users")]
    public async Task<ActionResult<ApiResponse<AdminUserListItemResponse>>> CreateUser([FromBody] AdminSaveUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(item => item.Errors).FirstOrDefault()?.ErrorMessage ?? "请求参数错误";
            return Ok(ApiResponse<AdminUserListItemResponse>.Fail(400, firstError));
        }
        var response = await _adminService.CreateUserAsync(request);
        return Ok(ApiResponse<AdminUserListItemResponse>.Success(response, "新增用户成功"));
    }

    [HttpPut("users/{id:long}")]
    public async Task<ActionResult<ApiResponse<AdminUserListItemResponse>>> UpdateUser(long id, [FromBody] AdminUpdateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(item => item.Errors).FirstOrDefault()?.ErrorMessage ?? "请求参数错误";
            return Ok(ApiResponse<AdminUserListItemResponse>.Fail(400, firstError));
        }
        var response = await _adminService.UpdateUserAsync(id, request);
        return Ok(ApiResponse<AdminUserListItemResponse>.Success(response, "编辑用户成功"));
    }

    [HttpDelete("users/{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(long id)
    {
        await _adminService.DeleteUserAsync(id);
        return Ok(ApiResponse<object>.Success(null, "删除用户成功"));
    }

    [HttpPost("users/batch-delete")]
    public async Task<ActionResult<ApiResponse<object>>> BatchDelete([FromBody] BatchDeleteUsersRequest request)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(item => item.Errors).FirstOrDefault()?.ErrorMessage ?? "请求参数错误";
            return Ok(ApiResponse<object>.Fail(400, firstError));
        }
        await _adminService.BatchDeleteUsersAsync(request.UserIds);
        return Ok(ApiResponse<object>.Success(null, "批量删除成功"));
    }

    [HttpGet("operation-record-cleanup-setting")]
    public async Task<ActionResult<ApiResponse<OperationRecordCleanupSettingResponse>>> GetOperationRecordCleanupSetting()
    {
        var response = await _adminService.GetOperationRecordCleanupSettingAsync();
        return Ok(ApiResponse<OperationRecordCleanupSettingResponse>.Success(response));
    }

    [HttpPut("operation-record-cleanup-setting")]
    public async Task<ActionResult<ApiResponse<OperationRecordCleanupSettingResponse>>> UpdateOperationRecordCleanupSetting([FromBody] UpdateOperationRecordCleanupSettingRequest request)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(item => item.Errors).FirstOrDefault()?.ErrorMessage ?? "请求参数错误";
            return Ok(ApiResponse<OperationRecordCleanupSettingResponse>.Fail(400, firstError));
        }
        var response = await _adminService.UpdateOperationRecordCleanupSettingAsync(request);
        return Ok(ApiResponse<OperationRecordCleanupSettingResponse>.Success(response, "操作记录自动清理配置保存成功"));
    }

    [HttpGet("export/users")]
    public async Task<IActionResult> ExportUsers()
    {
        var result = await _exportService.ExportUsersAsync();
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet("export/documents")]
    public async Task<IActionResult> ExportDocuments()
    {
        var result = await _exportService.ExportDocumentsAsync();
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet("export/scg")]
    public async Task<IActionResult> ExportScgRecords()
    {
        var result = await _exportService.ExportScgRecordsAsync();
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet("export/mr")]
    public async Task<IActionResult> ExportMrRecords()
    {
        var result = await _exportService.ExportMrRecordsAsync();
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet("backup/system")]
    public async Task<IActionResult> ExportSystemBackup()
    {
        var result = await _exportService.ExportSystemBackupAsync();
        return File(result.Content, result.ContentType, result.FileName);
    }
}
