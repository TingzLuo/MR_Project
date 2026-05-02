using MRProject.Api.Common;
using MRProject.Api.DTOs.Scg;
using MRProject.Api.Enums;
using MRProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MRProject.Api.Controllers;

[ApiController]
[Route("api/scg")]
[Authorize(Roles = nameof(UserRole.User))]
public class ScgController : ControllerBase
{
    private readonly IScgService _scgService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IExportService _exportService;

    public ScgController(IScgService scgService, ICurrentUserService currentUserService, IExportService exportService)
    {
        _scgService = scgService;
        _currentUserService = currentUserService;
        _exportService = exportService;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<ScgDetailResponse>>> Generate([FromBody] GenerateScgRequest request)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(item => item.Errors).FirstOrDefault()?.ErrorMessage ?? "请求参数错误";
            return Ok(ApiResponse<ScgDetailResponse>.Fail(400, firstError));
        }

        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _scgService.GenerateAsync(currentUser.UserId, request);
        return Ok(ApiResponse<ScgDetailResponse>.Success(response, "SCG 生成成功"));
    }

    [HttpGet("query")]
    public async Task<ActionResult<ApiResponse<ScgDetailResponse?>>> GetByDocuments([FromQuery] string documentIds)
    {
        var ids = documentIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(item => long.TryParse(item, out var id) ? id : 0).Where(item => item > 0).ToList();
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _scgService.GetByDocumentIdsAsync(currentUser.UserId, ids);
        return Ok(ApiResponse<ScgDetailResponse?>.Success(response));
    }

    [HttpGet("confirmed")]
    public async Task<ActionResult<ApiResponse<List<ConfirmedScgListItemResponse>>>> GetConfirmedList()
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _scgService.GetConfirmedListAsync(currentUser.UserId);
        return Ok(ApiResponse<List<ConfirmedScgListItemResponse>>.Success(response));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<ScgDetailResponse>>> Save(long id, [FromBody] SaveScgRequest request)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _scgService.SaveAsync(currentUser.UserId, id, request);
        return Ok(ApiResponse<ScgDetailResponse>.Success(response, "SCG 保存成功"));
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        await _scgService.DeleteAsync(currentUser.UserId, id);
        return Ok(ApiResponse<object>.Success(null, "SCG 删除成功"));
    }
    [HttpGet("{id:long}/export")]
    public async Task<IActionResult> Export(long id)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var result = await _exportService.ExportScgAsync(currentUser.UserId, id);
        return File(result.Content, result.ContentType, result.FileName);
    }
}

