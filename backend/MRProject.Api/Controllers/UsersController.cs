using MRProject.Api.Common;
using MRProject.Api.DTOs.Admin;
using MRProject.Api.DTOs.Users;
using MRProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MRProject.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(IUserService userService, ICurrentUserService currentUserService)
    {
        _userService = userService;
        _currentUserService = currentUserService;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<ProfileResponse>>> GetProfile()
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _userService.GetProfileAsync(currentUser.UserId);
        return Ok(ApiResponse<ProfileResponse>.Success(response));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<ProfileResponse>>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(item => item.Errors).FirstOrDefault()?.ErrorMessage ?? "请求参数错误";
            return Ok(ApiResponse<ProfileResponse>.Fail(400, firstError));
        }

        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _userService.UpdateProfileAsync(currentUser.UserId, request);
        return Ok(ApiResponse<ProfileResponse>.Success(response, "个人信息保存成功"));
    }

    [HttpPut("password")]
    public async Task<ActionResult<ApiResponse<object>>> UpdatePassword([FromBody] UpdatePasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(item => item.Errors).FirstOrDefault()?.ErrorMessage ?? "请求参数错误";
            return Ok(ApiResponse<object>.Fail(400, firstError));
        }

        var currentUser = _currentUserService.GetCurrentUser();
        await _userService.UpdatePasswordAsync(currentUser.UserId, request);
        return Ok(ApiResponse<object>.Success(null, "密码修改成功"));
    }

    [HttpGet("operation-record-cleanup-setting")]
    public async Task<ActionResult<ApiResponse<UserOperationCleanupSettingResponse>>> GetOperationRecordCleanupSetting()
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _userService.GetOperationRecordCleanupSettingAsync(currentUser.UserId);
        return Ok(ApiResponse<UserOperationCleanupSettingResponse>.Success(response));
    }

    [HttpPut("operation-record-cleanup-setting")]
    public async Task<ActionResult<ApiResponse<UserOperationCleanupSettingResponse>>> UpdateOperationRecordCleanupSetting([FromBody] UpdateUserOperationCleanupSettingRequest request)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(item => item.Errors).FirstOrDefault()?.ErrorMessage ?? "请求参数错误";
            return Ok(ApiResponse<UserOperationCleanupSettingResponse>.Fail(400, firstError));
        }

        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _userService.UpdateOperationRecordCleanupSettingAsync(currentUser.UserId, request);
        return Ok(ApiResponse<UserOperationCleanupSettingResponse>.Success(response, "个人操作记录自动清理配置保存成功"));
    }
}
