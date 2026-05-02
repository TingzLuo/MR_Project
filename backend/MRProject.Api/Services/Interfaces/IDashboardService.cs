using MRProject.Api.DTOs.Dashboard;
using MRProject.Api.Common;

namespace MRProject.Api.Services.Interfaces;

public interface IDashboardService
{
    Task<UserStatsResponse> GetUserStatsAsync(long userId);

    Task<List<RecentActivityResponse>> GetRecentActivitiesAsync(long userId);

    Task<PagedResult<RecentActivityResponse>> GetActivityPagedListAsync(long userId, int pageNumber, int pageSize);
}
