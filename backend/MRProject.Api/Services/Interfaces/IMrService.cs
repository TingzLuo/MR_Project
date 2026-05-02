using MRProject.Api.Common;
using MRProject.Api.DTOs.Mr;

namespace MRProject.Api.Services.Interfaces;

public interface IMrService
{
    Task<MrDetailResponse> GenerateAsync(long userId, GenerateMrRequest request);

    Task<PagedResult<MrListItemResponse>> GetPagedListAsync(long userId, int pageNumber, int pageSize);

    Task<MrDetailResponse> GetDetailAsync(long userId, long mrId);

    Task<MrDetailResponse?> GetByScgIdAsync(long userId, long scgId);

    Task<MrDetailResponse> SaveAsync(long userId, long mrId, SaveMrRequest request);

    Task<MrDetailResponse> AddItemAsync(long userId, long mrId, SaveMrItemRequest request);

    Task<MrDetailResponse> UpdateItemAsync(long userId, long mrId, string itemId, SaveMrItemRequest request);

    Task<MrDetailResponse> DeleteItemAsync(long userId, long mrId, string itemId);

    Task<List<MrHistoryItemResponse>> GetHistoryListAsync(long userId, long mrId);

    Task<MrHistoryDetailResponse> GetHistoryDetailAsync(long userId, long historyId);
}
