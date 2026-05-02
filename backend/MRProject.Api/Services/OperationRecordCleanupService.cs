using MRProject.Api.Common;
using MRProject.Api.Data;
using MRProject.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


namespace MRProject.Api.Services;

public class OperationRecordCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OperationRecordCleanupOptions _options;
    private readonly ILogger<OperationRecordCleanupService> _logger;

    public OperationRecordCleanupService(
        IServiceScopeFactory scopeFactory,
        IOptions<OperationRecordCleanupOptions> options,
        ILogger<OperationRecordCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollingInterval = TimeSpan.FromHours(1);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to cleanup operation records.");
            }

            await Task.Delay(pollingInterval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var globalSetting = await GetOrCreateGlobalSettingAsync(dbContext, cancellationToken);

        if (!globalSetting.Enabled)
        {
            _logger.LogInformation("Operation record cleanup is disabled.");
            return;
        }

        var now = SystemTime.Now();
        var userSettings = await dbContext.UserOperationCleanupSettings.ToListAsync(cancellationToken);
        var configuredUserIds = userSettings.Select(item => item.UserId).ToHashSet();
        var recordUserIds = await dbContext.UserOperationRecords.Select(item => item.UserId)
            .Concat(dbContext.ScgHistoryRecords.Select(item => item.UserId))
            .Concat(dbContext.MrHistoryRecords.Select(item => item.UserId))
            .Distinct()
            .ToListAsync(cancellationToken);

        var totalScgHistoryRemoved = 0;
        var totalMrHistoryRemoved = 0;
        var totalUserOperationRemoved = 0;

        if (IsCleanupDue(globalSetting.LastCleanupAt, globalSetting.IntervalValue, globalSetting.IntervalUnit, now))
        {
            var inheritedUserIds = recordUserIds.Where(item => !configuredUserIds.Contains(item)).ToList();
            if (inheritedUserIds.Count > 0)
            {
                var result = await CleanupUserRecordsAsync(dbContext, inheritedUserIds, now.AddDays(-Math.Max(1, globalSetting.RetentionDays)), cancellationToken);
                totalScgHistoryRemoved += result.ScgHistoryRemoved;
                totalMrHistoryRemoved += result.MrHistoryRemoved;
                totalUserOperationRemoved += result.UserOperationRemoved;
            }
            globalSetting.LastCleanupAt = now;
        }

        foreach (var userSetting in userSettings)
        {
            if (!userSetting.Enabled)
            {
                continue;
            }

            if (!IsCleanupDue(userSetting.LastCleanupAt, userSetting.IntervalValue, userSetting.IntervalUnit, now))
            {
                continue;
            }

            var result = await CleanupUserRecordsAsync(dbContext, [userSetting.UserId], now.AddDays(-Math.Max(1, userSetting.RetentionDays)), cancellationToken);
            totalScgHistoryRemoved += result.ScgHistoryRemoved;
            totalMrHistoryRemoved += result.MrHistoryRemoved;
            totalUserOperationRemoved += result.UserOperationRemoved;
            userSetting.LastCleanupAt = now;
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Operation record cleanup completed. Removed ScgHistory={ScgHistoryCount}, MrHistory={MrHistoryCount}, UserOperations={UserOperationCount}",
            totalScgHistoryRemoved,
            totalMrHistoryRemoved,
            totalUserOperationRemoved);
    }

    private async Task<(int ScgHistoryRemoved, int MrHistoryRemoved, int UserOperationRemoved)> CleanupUserRecordsAsync(
        ApplicationDbContext dbContext,
        List<long> userIds,
        DateTime cutoffTime,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return (0, 0, 0);
        }

        var scgHistoryRecords = await dbContext.ScgHistoryRecords
            .Where(item => userIds.Contains(item.UserId) && item.CreatedAt < cutoffTime)
            .ToListAsync(cancellationToken);
        if (scgHistoryRecords.Count > 0)
        {
            dbContext.ScgHistoryRecords.RemoveRange(scgHistoryRecords);
        }

        var mrHistoryRecords = await dbContext.MrHistoryRecords
            .Where(item => userIds.Contains(item.UserId) && item.CreatedAt < cutoffTime)
            .ToListAsync(cancellationToken);
        if (mrHistoryRecords.Count > 0)
        {
            dbContext.MrHistoryRecords.RemoveRange(mrHistoryRecords);
        }

        var userOperationRecords = await dbContext.UserOperationRecords
            .Where(item => userIds.Contains(item.UserId) && item.CreatedAt < cutoffTime)
            .ToListAsync(cancellationToken);
        if (userOperationRecords.Count > 0)
        {
            dbContext.UserOperationRecords.RemoveRange(userOperationRecords);
        }

        return (scgHistoryRecords.Count, mrHistoryRecords.Count, userOperationRecords.Count);
    }

    private async Task<OperationRecordCleanupSetting> GetOrCreateGlobalSettingAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var setting = await dbContext.OperationRecordCleanupSettings.OrderBy(item => item.Id).FirstOrDefaultAsync(cancellationToken);
        if (setting is not null)
        {
            return setting;
        }

        setting = new OperationRecordCleanupSetting
        {
            Enabled = _options.Enabled,
            RetentionDays = _options.RetentionDays,
            IntervalValue = _options.IntervalValue,
            IntervalUnit = NormalizeIntervalUnit(_options.IntervalUnit),
            CreatedAt = SystemTime.Now(),
            UpdatedAt = SystemTime.Now()
        };
        dbContext.OperationRecordCleanupSettings.Add(setting);
        await dbContext.SaveChangesAsync(cancellationToken);
        return setting;
    }

    private static bool IsCleanupDue(DateTime? lastCleanupAt, int intervalValue, string intervalUnit, DateTime now)
    {
        if (!lastCleanupAt.HasValue)
        {
            return true;
        }

        var safeIntervalValue = intervalValue <= 0 ? 1 : intervalValue;
        var unit = NormalizeIntervalUnit(intervalUnit);
        var nextCleanupTime = unit switch
        {
            "day" => lastCleanupAt.Value.AddDays(safeIntervalValue),
            "week" => lastCleanupAt.Value.AddDays(7 * safeIntervalValue),
            "month" => lastCleanupAt.Value.AddMonths(safeIntervalValue),
            _ => lastCleanupAt.Value.AddHours(safeIntervalValue)
        };

        return now >= nextCleanupTime;
    }

    private static string NormalizeIntervalUnit(string intervalUnit)
    {
        return intervalUnit?.Trim().ToLowerInvariant() switch
        {
            "day" => "day",
            "week" => "week",
            "month" => "month",
            _ => "hour"
        };
    }
}

