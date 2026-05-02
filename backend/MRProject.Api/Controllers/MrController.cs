using MRProject.Api.Common;
using MRProject.Api.DTOs.Mr;
using MRProject.Api.Enums;
using MRProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MRProject.Api.Controllers;

[ApiController]
[Route("api/mr")]
[Authorize(Roles = nameof(UserRole.User))]
public class MrController : ControllerBase
{
    private readonly IMrService _mrService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IExportService _exportService;

    public MrController(IMrService mrService, ICurrentUserService currentUserService, IExportService exportService)
    {
        _mrService = mrService;
        _currentUserService = currentUserService;
        _exportService = exportService;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<MrDetailResponse>>> Generate([FromBody] GenerateMrRequest request)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(item => item.Errors).FirstOrDefault()?.ErrorMessage ?? "请求参数错误";
            return Ok(ApiResponse<MrDetailResponse>.Fail(400, firstError));
        }
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _mrService.GenerateAsync(currentUser.UserId, request);
        return Ok(ApiResponse<MrDetailResponse>.Success(response, "蜕变关系生成成功"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<MrListItemResponse>>>> GetPagedList([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _mrService.GetPagedListAsync(currentUser.UserId, pageNumber, pageSize);
        return Ok(ApiResponse<PagedResult<MrListItemResponse>>.Success(response));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<MrDetailResponse>>> GetDetail(long id)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _mrService.GetDetailAsync(currentUser.UserId, id);
        return Ok(ApiResponse<MrDetailResponse>.Success(response));
    }

    [HttpGet("scg/{scgId:long}")]
    public async Task<ActionResult<ApiResponse<MrDetailResponse?>>> GetByScgId(long scgId)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _mrService.GetByScgIdAsync(currentUser.UserId, scgId);
        return Ok(ApiResponse<MrDetailResponse?>.Success(response));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<MrDetailResponse>>> Save(long id, [FromBody] SaveMrRequest request)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _mrService.SaveAsync(currentUser.UserId, id, request);
        return Ok(ApiResponse<MrDetailResponse>.Success(response, "蜕变关系保存成功"));
    }

    [HttpPost("{id:long}/items")]
    public async Task<ActionResult<ApiResponse<MrDetailResponse>>> AddItem(long id, [FromBody] SaveMrItemRequest request)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _mrService.AddItemAsync(currentUser.UserId, id, request);
        return Ok(ApiResponse<MrDetailResponse>.Success(response, "蜕变关系新增成功"));
    }

    [HttpPut("{id:long}/items/{itemId}")]
    public async Task<ActionResult<ApiResponse<MrDetailResponse>>> UpdateItem(long id, string itemId, [FromBody] SaveMrItemRequest request)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _mrService.UpdateItemAsync(currentUser.UserId, id, itemId, request);
        return Ok(ApiResponse<MrDetailResponse>.Success(response, "蜕变关系修改成功"));
    }

    [HttpDelete("{id:long}/items/{itemId}")]
    public async Task<ActionResult<ApiResponse<MrDetailResponse>>> DeleteItem(long id, string itemId)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _mrService.DeleteItemAsync(currentUser.UserId, id, itemId);
        return Ok(ApiResponse<MrDetailResponse>.Success(response, "蜕变关系删除成功"));
    }

    [HttpGet("{id:long}/history")]
    public async Task<ActionResult<ApiResponse<List<MrHistoryItemResponse>>>> GetHistory(long id)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _mrService.GetHistoryListAsync(currentUser.UserId, id);
        return Ok(ApiResponse<List<MrHistoryItemResponse>>.Success(response));
    }

    [HttpGet("history/{historyId:long}")]
    public async Task<ActionResult<ApiResponse<MrHistoryDetailResponse>>> GetHistoryDetail(long historyId)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _mrService.GetHistoryDetailAsync(currentUser.UserId, historyId);
        return Ok(ApiResponse<MrHistoryDetailResponse>.Success(response));
    }

    [HttpGet("{id:long}/export")]
    public async Task<IActionResult> Export(long id)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var result = await _exportService.ExportMrAsync(currentUser.UserId, id);
        return File(result.Content, result.ContentType, result.FileName);
    }
}
