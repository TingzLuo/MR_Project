using MRProject.Api.Common;
using MRProject.Api.DTOs.Dashboard;
using MRProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MRProject.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUserService;

    public DashboardController(IDashboardService dashboardService, ICurrentUserService currentUserService)
    {
        _dashboardService = dashboardService;
        _currentUserService = currentUserService;
    }

    [HttpGet("user-stats")]
    public async Task<ActionResult<ApiResponse<UserStatsResponse>>> GetUserStats()
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _dashboardService.GetUserStatsAsync(currentUser.UserId);
        return Ok(ApiResponse<UserStatsResponse>.Success(response));
    }

    [HttpGet("recent-activities")]
    public async Task<ActionResult<ApiResponse<List<RecentActivityResponse>>>> GetRecentActivities()
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _dashboardService.GetRecentActivitiesAsync(currentUser.UserId);
        return Ok(ApiResponse<List<RecentActivityResponse>>.Success(response));
    }

    [HttpGet("activities")]
    public async Task<ActionResult<ApiResponse<PagedResult<RecentActivityResponse>>>> GetActivities([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var currentUser = _currentUserService.GetCurrentUser();
        var response = await _dashboardService.GetActivityPagedListAsync(currentUser.UserId, pageNumber, pageSize);
        return Ok(ApiResponse<PagedResult<RecentActivityResponse>>.Success(response));
    }
}
