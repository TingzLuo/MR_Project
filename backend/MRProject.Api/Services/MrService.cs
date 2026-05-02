using MRProject.Api.Common;
using MRProject.Api.Data;
using MRProject.Api.DTOs.Mr;
using MRProject.Api.Entities;
using MRProject.Api.Enums;
using MRProject.Api.Services.Interfaces;
using MRProject.Api.Services.Llm;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MRProject.Api.Services;

public class MrService : IMrService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILlmService _llmService;

    public MrService(ApplicationDbContext dbContext, ILlmService llmService)
    {
        _dbContext = dbContext;
        _llmService = llmService;
    }

    public async Task<MrDetailResponse> GenerateAsync(long userId, GenerateMrRequest request)
    {
        var scgRecord = await _dbContext.ScgRecords.AsNoTracking().FirstOrDefaultAsync(item => item.Id == request.ScgId && item.UserId == userId && !item.IsDeleted);
        if (scgRecord is null) throw new AppException("SCG 不存在", 404);
        if (!scgRecord.IsConfirmed) throw new AppException("SCG 尚未确认，不能生成蜕变关系", 409);

        var prompt = MrPromptTemplate.Build(scgRecord.DocumentNamesSummary, scgRecord.ScgJson);
        var log = new LlmCallLog { BusinessType = "mr_generate", BusinessId = scgRecord.Id, PromptContent = prompt, CallStatus = "success", CreatedAt = SystemTime.Now(), UpdatedAt = SystemTime.Now(), ErrorMessage = string.Empty };

        List<MrItemDto> mrItems;
        try
        {
            mrItems = await _llmService.GenerateMrAsync(prompt, scgRecord.ScgJson, scgRecord.DocumentNamesSummary);
            ValidateMrItems(mrItems);
            log.ResponseContent = JsonSerializer.Serialize(mrItems);
        }
        catch (Exception exception)
        {
            log.CallStatus = "failed";
            log.ErrorMessage = exception.Message;
            log.UpdatedAt = SystemTime.Now();
            _dbContext.LlmCallLogs.Add(log);
            await _dbContext.SaveChangesAsync();
            throw;
        }

        _dbContext.LlmCallLogs.Add(log);

        var now = SystemTime.Now();
        var mrJson = JsonSerializer.Serialize(mrItems);
        var mrRecord = await _dbContext.MrRecords.FirstOrDefaultAsync(item => item.ScgRecordId == scgRecord.Id && item.UserId == userId && !item.IsDeleted);

        if (mrRecord is null)
        {
            mrRecord = new MrRecord { ScgRecordId = scgRecord.Id, UserId = userId, DocumentIdsKey = scgRecord.DocumentIdsKey, DocumentNamesSummary = scgRecord.DocumentNamesSummary, MrJson = mrJson, CreatedAt = now, UpdatedAt = now, IsDeleted = false };
            _dbContext.MrRecords.Add(mrRecord);
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            mrRecord.DocumentIdsKey = scgRecord.DocumentIdsKey;
            mrRecord.DocumentNamesSummary = scgRecord.DocumentNamesSummary;
            mrRecord.MrJson = mrJson;
            mrRecord.UpdatedAt = now;
            await _dbContext.SaveChangesAsync();
        }

        await AddHistorySnapshotAsync(mrRecord.Id, userId, "generate", mrJson);
        await UpdateDocumentStatusAsync(userId, scgRecord.DocumentIdsKey, now, DocumentProcessStatus.MrGenerated);
        return MapDetail(mrRecord, mrItems);
    }

    public async Task<PagedResult<MrListItemResponse>> GetPagedListAsync(long userId, int pageNumber, int pageSize)
    {
        var safePageNumber = pageNumber <= 0 ? 1 : pageNumber;
        var safePageSize = pageSize <= 0 ? 10 : pageSize;
        var query = _dbContext.MrRecords.AsNoTracking().Where(item => item.UserId == userId && !item.IsDeleted).OrderByDescending(item => item.UpdatedAt);
        var total = await query.CountAsync();
        var records = await query.Skip((safePageNumber - 1) * safePageSize).Take(safePageSize).ToListAsync();
        var items = records.Select(record => new MrListItemResponse { Id = record.Id, ScgId = record.ScgRecordId, DocumentNamesSummary = record.DocumentNamesSummary, ItemCount = ParseMrItems(record.MrJson).Count, CreatedAt = record.CreatedAt, UpdatedAt = record.UpdatedAt }).ToList();
        return new PagedResult<MrListItemResponse> { Items = items, Total = total, PageNumber = safePageNumber, PageSize = safePageSize };
    }

    public async Task<MrDetailResponse> GetDetailAsync(long userId, long mrId)
    {
        var record = await _dbContext.MrRecords.AsNoTracking().FirstOrDefaultAsync(item => item.Id == mrId && item.UserId == userId && !item.IsDeleted);
        if (record is null) throw new AppException("蜕变关系不存在", 404);
        return MapDetail(record, ParseMrItems(record.MrJson));
    }

    public async Task<MrDetailResponse?> GetByScgIdAsync(long userId, long scgId)
    {
        var record = await _dbContext.MrRecords.AsNoTracking().FirstOrDefaultAsync(item => item.ScgRecordId == scgId && item.UserId == userId && !item.IsDeleted);
        return record is null ? null : MapDetail(record, ParseMrItems(record.MrJson));
    }

    public async Task<MrDetailResponse> SaveAsync(long userId, long mrId, SaveMrRequest request)
    {
        ValidateMrItems(request.MrItems);
        var record = await GetEditableMrRecordAsync(userId, mrId);
        record.MrJson = JsonSerializer.Serialize(request.MrItems);
        record.UpdatedAt = SystemTime.Now();
        await _dbContext.SaveChangesAsync();
        await AddHistorySnapshotAsync(record.Id, userId, "save", record.MrJson);
        return MapDetail(record, request.MrItems);
    }

    public async Task<MrDetailResponse> AddItemAsync(long userId, long mrId, SaveMrItemRequest request)
    {
        var record = await GetEditableMrRecordAsync(userId, mrId);
        var items = ParseMrItems(record.MrJson);
        var newItem = MapItem(request);
        if (items.Any(item => item.Id == newItem.Id)) throw new AppException("MR ID 已存在", 409);
        items.Add(newItem);
        ValidateMrItems(items);
        record.MrJson = JsonSerializer.Serialize(items);
        record.UpdatedAt = SystemTime.Now();
        await _dbContext.SaveChangesAsync();
        await AddHistorySnapshotAsync(record.Id, userId, "add", record.MrJson);
        return MapDetail(record, items);
    }

    public async Task<MrDetailResponse> UpdateItemAsync(long userId, long mrId, string itemId, SaveMrItemRequest request)
    {
        var record = await GetEditableMrRecordAsync(userId, mrId);
        var items = ParseMrItems(record.MrJson);
        var index = items.FindIndex(item => item.Id == itemId);
        if (index < 0) throw new AppException("蜕变关系条目不存在", 404);
        var updatedItem = MapItem(request);
        if (!string.Equals(itemId, updatedItem.Id, StringComparison.OrdinalIgnoreCase) && items.Any(item => item.Id == updatedItem.Id)) throw new AppException("MR ID 已存在", 409);
        items[index] = updatedItem;
        ValidateMrItems(items);
        record.MrJson = JsonSerializer.Serialize(items);
        record.UpdatedAt = SystemTime.Now();
        await _dbContext.SaveChangesAsync();
        await AddHistorySnapshotAsync(record.Id, userId, "update", record.MrJson);
        return MapDetail(record, items);
    }

    public async Task<MrDetailResponse> DeleteItemAsync(long userId, long mrId, string itemId)
    {
        var record = await GetEditableMrRecordAsync(userId, mrId);
        var items = ParseMrItems(record.MrJson);
        var removed = items.RemoveAll(item => item.Id == itemId);
        if (removed == 0) throw new AppException("蜕变关系条目不存在", 404);
        ValidateMrItems(items);
        record.MrJson = JsonSerializer.Serialize(items);
        record.UpdatedAt = SystemTime.Now();
        await _dbContext.SaveChangesAsync();
        await AddHistorySnapshotAsync(record.Id, userId, "delete", record.MrJson);
        return MapDetail(record, items);
    }

    public async Task<List<MrHistoryItemResponse>> GetHistoryListAsync(long userId, long mrId)
    {
        var exists = await _dbContext.MrRecords.AnyAsync(item => item.Id == mrId && item.UserId == userId && !item.IsDeleted);
        if (!exists) throw new AppException("蜕变关系不存在", 404);
        return await _dbContext.MrHistoryRecords.AsNoTracking().Where(item => item.MrRecordId == mrId && item.UserId == userId).OrderByDescending(item => item.CreatedAt).Select(item => new MrHistoryItemResponse { Id = item.Id, OperationType = item.OperationType, CreatedAt = item.CreatedAt }).ToListAsync();
    }

    public async Task<MrHistoryDetailResponse> GetHistoryDetailAsync(long userId, long historyId)
    {
        var record = await _dbContext.MrHistoryRecords.AsNoTracking().FirstOrDefaultAsync(item => item.Id == historyId && item.UserId == userId);
        if (record is null) throw new AppException("历史记录不存在", 404);
        return new MrHistoryDetailResponse { Id = record.Id, MrRecordId = record.MrRecordId, OperationType = record.OperationType, MrItems = ParseMrItems(record.MrJson), CreatedAt = record.CreatedAt };
    }

    private async Task<MrRecord> GetEditableMrRecordAsync(long userId, long mrId)
    {
        var record = await _dbContext.MrRecords.FirstOrDefaultAsync(item => item.Id == mrId && item.UserId == userId && !item.IsDeleted);
        if (record is null) throw new AppException("蜕变关系不存在", 404);
        return record;
    }

    private async Task AddHistorySnapshotAsync(long mrRecordId, long userId, string operationType, string mrJson)
    {
        _dbContext.MrHistoryRecords.Add(new MrHistoryRecord { MrRecordId = mrRecordId, UserId = userId, OperationType = operationType, MrJson = mrJson, CreatedAt = SystemTime.Now(), UpdatedAt = SystemTime.Now() });
        await _dbContext.SaveChangesAsync();
    }

    private async Task UpdateDocumentStatusAsync(long userId, string documentIdsKey, DateTime updatedAt, DocumentProcessStatus processStatus)
    {
        var documentIds = documentIdsKey.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var documents = await _dbContext.Documents.Where(item => item.UserId == userId && !item.IsDeleted && documentIds.Contains(item.Id.ToString())).ToListAsync();
        foreach (var document in documents)
        {
            document.ProcessStatus = processStatus;
            document.UpdatedAt = updatedAt;
        }
        await _dbContext.SaveChangesAsync();
    }

    private static MrItemDto MapItem(SaveMrItemRequest request)
    {
        return new MrItemDto { Id = request.Id.Trim(), InputRelation = request.InputRelation.Trim(), OutputRelation = request.OutputRelation.Trim(), Description = request.Description.Trim() };
    }

    private static void ValidateMrItems(List<MrItemDto> mrItems)
    {
        if (mrItems.Count == 0) throw new AppException("蜕变关系不能为空");
        var ids = new HashSet<string>();
        foreach (var item in mrItems)
        {
            if (string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.InputRelation) || string.IsNullOrWhiteSpace(item.OutputRelation) || string.IsNullOrWhiteSpace(item.Description)) throw new AppException("蜕变关系结构不合法");
            if (!ids.Add(item.Id)) throw new AppException("MR ID 不能重复");
        }
    }

    private static List<MrItemDto> ParseMrItems(string mrJson)
    {
        return JsonSerializer.Deserialize<List<MrItemDto>>(mrJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    private static MrDetailResponse MapDetail(MrRecord record, List<MrItemDto> mrItems)
    {
        return new MrDetailResponse { Id = record.Id, ScgId = record.ScgRecordId, DocumentNamesSummary = record.DocumentNamesSummary, MrItems = mrItems, CreatedAt = record.CreatedAt, UpdatedAt = record.UpdatedAt };
    }
}
