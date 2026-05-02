using MRProject.Api.DTOs.Scg;
using MRProject.Api.DTOs.Mr;

namespace MRProject.Api.Services.Interfaces;

public interface IScgService
{
    Task<ScgDetailResponse> GenerateAsync(long userId, GenerateScgRequest request);

    Task<ScgDetailResponse?> GetByDocumentIdsAsync(long userId, List<long> documentIds);

    Task<List<ConfirmedScgListItemResponse>> GetConfirmedListAsync(long userId);

    Task<ScgDetailResponse> SaveAsync(long userId, long scgId, SaveScgRequest request);

    Task<ScgDetailResponse> ConfirmAsync(long userId, long scgId);

    Task DeleteAsync(long userId, long scgId);
}
