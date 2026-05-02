using MRProject.Api.Common;
using MRProject.Api.DTOs.Documents;
using Microsoft.AspNetCore.Http;

namespace MRProject.Api.Services.Interfaces;

public interface IDocumentService
{
    Task<UploadDocumentsResponse> UploadAsync(long userId, List<IFormFile> files);

    Task<PagedResult<DocumentItemResponse>> GetPagedListAsync(long userId, DocumentQueryRequest request);

    Task<DocumentDetailResponse> GetDetailAsync(long userId, long documentId);

    Task DeleteAsync(long userId, long documentId);
}
