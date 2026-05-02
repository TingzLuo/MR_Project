using MRProject.Api.Common;
using MRProject.Api.DTOs.Documents;
using MRProject.Api.Enums;
using MRProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MRProject.Api.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize(Roles = nameof(UserRole.User))]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ICurrentUserService _currentUserService;

    public DocumentsController(IDocumentService documentService, ICurrentUserService currentUserService)
    {
        _documentService = documentService;
        _currentUserService = currentUserService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<UploadDocumentsResponse>>> Upload([FromForm] List<IFormFile> files)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _documentService.UploadAsync(currentUser.UserId, files);
        return Ok(ApiResponse<UploadDocumentsResponse>.Success(response, "上传成功"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<DocumentItemResponse>>>> GetPagedList([FromQuery] DocumentQueryRequest request)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _documentService.GetPagedListAsync(currentUser.UserId, request);
        return Ok(ApiResponse<PagedResult<DocumentItemResponse>>.Success(response));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<DocumentDetailResponse>>> GetDetail(long id)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _documentService.GetDetailAsync(currentUser.UserId, id);
        return Ok(ApiResponse<DocumentDetailResponse>.Success(response));
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        await _documentService.DeleteAsync(currentUser.UserId, id);
        return Ok(ApiResponse<object>.Success(null, "删除成功"));
    }
}
