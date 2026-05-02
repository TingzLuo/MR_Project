using MRProject.Api.Data;
using MRProject.Api.DTOs.Dashboard;
using MRProject.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using MRProject.Api.Common;

namespace MRProject.Api.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserStatsResponse> GetUserStatsAsync(long userId)
    {
        var fileCount = await _dbContext.Documents.CountAsync(item => item.UserId == userId && !item.IsDeleted);
        var scgCount = await _dbContext.ScgRecords.CountAsync(item => item.UserId == userId && !item.IsDeleted);
        var mrCount = await _dbContext.MrRecords.CountAsync(item => item.UserId == userId && !item.IsDeleted);

        var userDocumentIds = await _dbContext.Documents
            .Where(item => item.UserId == userId && !item.IsDeleted)
            .Select(item => item.Id)
            .ToListAsync();

        var userScgIds = await _dbContext.ScgRecords
            .Where(item => item.UserId == userId && !item.IsDeleted)
            .Select(item => item.Id)
            .ToListAsync();

        var llmCallCount = await _dbContext.LlmCallLogs.CountAsync(item =>
            (item.BusinessType == "scg_generate" && userDocumentIds.Contains(item.BusinessId)) ||
            (item.BusinessType == "mr_generate" && userScgIds.Contains(item.BusinessId)));

        return new UserStatsResponse
        {
            FileCount = fileCount,
            LlmCallCount = llmCallCount,
            ScgCount = scgCount,
            MrCount = mrCount
        };
    }

    public async Task<List<RecentActivityResponse>> GetRecentActivitiesAsync(long userId)
    {
        var allActivities = await BuildActivitiesAsync(userId);
        return allActivities.Take(5).ToList();
    }

    public async Task<PagedResult<RecentActivityResponse>> GetActivityPagedListAsync(long userId, int pageNumber, int pageSize)
    {
        var safePageNumber = pageNumber <= 0 ? 1 : pageNumber;
        var safePageSize = pageSize <= 0 ? 10 : pageSize;
        var allActivities = await BuildActivitiesAsync(userId);
        var total = allActivities.Count;
        var items = allActivities
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        return new PagedResult<RecentActivityResponse>
        {
            Items = items,
            Total = total,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    private async Task<List<RecentActivityResponse>> BuildActivitiesAsync(long userId)
    {
        var uploadActivities = await _dbContext.Documents
            .AsNoTracking()
            .Where(item => item.UserId == userId && !item.IsDeleted)
            .Select(item => new ActivityRecord
            {
                FileName = item.OriginalFileName,
                ActionType = "upload",
                CreatedAt = item.CreatedAt
            })
            .ToListAsync();

        var scgActivities = await _dbContext.ScgHistoryRecords
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => new ActivityRecord
            {
                FileName = item.ScgName,
                ActionType = MapScgActionType(item.OperationType),
                CreatedAt = item.CreatedAt
            })
            .ToListAsync();

        var mrActivities = await _dbContext.MrHistoryRecords
            .AsNoTracking()
            .Join(_dbContext.MrRecords.AsNoTracking(),
                history => history.MrRecordId,
                mr => mr.Id,
                (history, mr) => new { history, mr })
            .Where(item => item.history.UserId == userId && !item.mr.IsDeleted)
            .Select(item => new ActivityRecord
            {
                FileName = item.mr.DocumentNamesSummary,
                ActionType = MapMrActionType(item.history.OperationType),
                CreatedAt = item.history.CreatedAt
            })
            .ToListAsync();

        var userActivities = await _dbContext.UserOperationRecords
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => new ActivityRecord
            {
                FileName = item.OperationTarget,
                ActionType = MapUserActionType(item.OperationType),
                CreatedAt = item.CreatedAt
            })
            .ToListAsync();

        return uploadActivities
            .Concat(scgActivities)
            .Concat(mrActivities)
            .Concat(userActivities)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new RecentActivityResponse
            {
                FileName = item.FileName,
                ActionType = item.ActionType,
                Time = item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            })
            .ToList();
    }

    private static string MapScgActionType(string operationType)
    {
        return operationType switch
        {
            "generate" => "generateScg",
            "save" => "saveScg",
            "update" => "updateScg",
            _ => $"scg_{operationType}"
        };
    }

    private static string MapMrActionType(string operationType)
    {
        return operationType switch
        {
            "generate" => "generateMr",
            "save" => "saveMr",
            "add" => "addMr",
            "update" => "updateMr",
            "delete" => "deleteMr",
            _ => $"mr_{operationType}"
        };
    }

    private static string MapUserActionType(string operationType)
    {
        return operationType switch
        {
            "updateProfile" => "updateProfile",
            "updatePassword" => "updatePassword",
            _ => $"user_{operationType}"
        };
    }

    private sealed class ActivityRecord
    {
        public string FileName { get; set; } = string.Empty;

        public string ActionType { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
