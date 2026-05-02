using MRProject.Api.Common;
using MRProject.Api.Data;
using MRProject.Api.DTOs.Scg;
using MRProject.Api.Entities;
using MRProject.Api.Enums;
using MRProject.Api.Services.Interfaces;
using MRProject.Api.Services.Llm;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MRProject.Api.Services;

public class ScgService : IScgService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDocumentParserService _documentParserService;
    private readonly ILlmService _llmService;

    public ScgService(
        ApplicationDbContext dbContext,
        IDocumentParserService documentParserService,
        ILlmService llmService)
    {
        _dbContext = dbContext;
        _documentParserService = documentParserService;
        _llmService = llmService;
    }

    public async Task<ScgDetailResponse> GenerateAsync(long userId, GenerateScgRequest request)
    {
        await EnsureScgNameColumnReadyAsync();

        var normalizedDocumentIds = NormalizeDocumentIds(request.DocumentIds);
        var documents = await _dbContext.Documents
            .Where(item => normalizedDocumentIds.Contains(item.Id) && item.UserId == userId && !item.IsDeleted)
            .OrderBy(item => item.Id)
            .ToListAsync();

        if (documents.Count != normalizedDocumentIds.Count)
        {
            throw new AppException("存在无效文件或文件不属于当前用户", 404);
        }

        var parsedSections = new List<string>();
        foreach (var document in documents)
        {
            var physicalPath = GetPhysicalPath(document.FilePath);
            if (!File.Exists(physicalPath))
            {
                throw new AppException($"文件 {document.OriginalFileName} 内容不存在，无法生成 SCG", 404);
            }

            var parsedText = await _documentParserService.ParseAsync(physicalPath, document.FileType);
            parsedSections.Add($"【文件：{document.DocumentName}】\n{parsedText}");
        }

        var sourceText = string.Join("\n\n", parsedSections);
        var documentNamesSummary = string.Join(" / ", documents.Select(item => item.DocumentName));
        var prompt = ScgPromptTemplate.Build(documentNamesSummary, sourceText);
        var primaryDocument = documents.First();
        var documentIdsKey = BuildDocumentIdsKey(normalizedDocumentIds);
        var log = new LlmCallLog
        {
            BusinessType = "scg_generate",
            BusinessId = primaryDocument.Id,
            PromptContent = prompt,
            CallStatus = "success",
            CreatedAt = SystemTime.Now(),
            UpdatedAt = SystemTime.Now(),
            ErrorMessage = string.Empty
        };

        ScgGraphDto graph;
        try
        {
            graph = await _llmService.GenerateScgAsync(prompt, sourceText, documentNamesSummary);
            ValidateGraph(graph);
            log.ResponseContent = JsonSerializer.Serialize(graph);
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
        var scgJson = JsonSerializer.Serialize(graph);
        var version = await GetNextScgVersionAsync(userId, documentIdsKey);
        var scgName = BuildScgName(documents, version);
        var scgRecord = new ScgRecord
        {
            DocumentId = primaryDocument.Id,
            UserId = userId,
            ScgName = scgName,
            DocumentIdsKey = documentIdsKey,
            DocumentNamesSummary = documentNamesSummary,
            ScgJson = scgJson,
            SourceTextSnapshot = sourceText,
            IsConfirmed = false,
            ConfirmedAt = null,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
        _dbContext.ScgRecords.Add(scgRecord);

        foreach (var document in documents)
        {
            document.ProcessStatus = DocumentProcessStatus.ScgGenerated;
            document.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync();
        await AddHistorySnapshotAsync(scgRecord.Id, userId, scgRecord.ScgName, "generate", scgJson);

        return MapDetail(scgRecord, primaryDocument.DocumentName, normalizedDocumentIds, graph);
    }

    public async Task<ScgDetailResponse?> GetByDocumentIdsAsync(long userId, List<long> documentIds)
    {
        await EnsureScgNameColumnReadyAsync();

        var normalizedDocumentIds = NormalizeDocumentIds(documentIds);
        if (normalizedDocumentIds.Count == 0)
        {
            return null;
        }

        var documentIdsKey = BuildDocumentIdsKey(normalizedDocumentIds);
        var record = await _dbContext.ScgRecords
            .AsNoTracking()
            .Join(_dbContext.Documents.AsNoTracking(),
                scg => scg.DocumentId,
                document => document.Id,
                (scg, document) => new { scg, document })
            .Where(item => item.scg.UserId == userId && !item.scg.IsDeleted && !item.document.IsDeleted && (item.scg.DocumentIdsKey == documentIdsKey || (normalizedDocumentIds.Count == 1 && item.scg.DocumentId == normalizedDocumentIds[0])))
            .OrderByDescending(item => item.scg.CreatedAt)
            .ThenByDescending(item => item.scg.Id)
            .FirstOrDefaultAsync();

        if (record is null)
        {
            return null;
        }

        var graph = JsonSerializer.Deserialize<ScgGraphDto>(record.scg.ScgJson) ?? new ScgGraphDto();
        return MapDetail(record.scg, record.document.DocumentName, ParseDocumentIds(record.scg.DocumentIdsKey, record.scg.DocumentId), graph);
    }

    public async Task<List<ConfirmedScgListItemResponse>> GetConfirmedListAsync(long userId)
    {
        await EnsureScgNameColumnReadyAsync();

        return await _dbContext.ScgRecords
            .AsNoTracking()
            .Where(item => item.UserId == userId && !item.IsDeleted && item.IsConfirmed)
            .OrderByDescending(item => item.ConfirmedAt)
            .ThenByDescending(item => item.CreatedAt)
            .Select(item => new ConfirmedScgListItemResponse
            {
                Id = item.Id,
                ScgName = item.ScgName,
                DocumentNamesSummary = item.DocumentNamesSummary,
                ConfirmedAt = item.ConfirmedAt,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<ScgDetailResponse> SaveAsync(long userId, long scgId, SaveScgRequest request)
    {
        await EnsureScgNameColumnReadyAsync();

        if (request.ScgGraph is null)
        {
            throw new AppException("SCG 图数据不能为空");
        }

        ValidateGraph(request.ScgGraph);

        var scgRecord = await _dbContext.ScgRecords
            .Join(_dbContext.Documents,
                scg => scg.DocumentId,
                document => document.Id,
                (scg, document) => new { scg, document })
            .FirstOrDefaultAsync(item => item.scg.Id == scgId && item.scg.UserId == userId && !item.scg.IsDeleted && !item.document.IsDeleted);

        if (scgRecord is null)
        {
            throw new AppException("SCG 不存在", 404);
        }

        var wasConfirmed = scgRecord.scg.IsConfirmed;
        var now = SystemTime.Now();
        scgRecord.scg.ScgJson = JsonSerializer.Serialize(request.ScgGraph);
        scgRecord.scg.IsConfirmed = true;
        scgRecord.scg.ConfirmedAt = now;
        scgRecord.scg.UpdatedAt = now;
        scgRecord.document.ProcessStatus = DocumentProcessStatus.ScgConfirmed;
        scgRecord.document.UpdatedAt = now;
        await _dbContext.SaveChangesAsync();
        await AddHistorySnapshotAsync(scgRecord.scg.Id, userId, scgRecord.scg.ScgName, wasConfirmed ? "update" : "save", scgRecord.scg.ScgJson);

        return MapDetail(scgRecord.scg, scgRecord.document.DocumentName, ParseDocumentIds(scgRecord.scg.DocumentIdsKey, scgRecord.scg.DocumentId), request.ScgGraph);
    }

    public async Task<ScgDetailResponse> ConfirmAsync(long userId, long scgId)
    {
        await EnsureScgNameColumnReadyAsync();

        var scgRecord = await _dbContext.ScgRecords
            .Join(_dbContext.Documents,
                scg => scg.DocumentId,
                document => document.Id,
                (scg, document) => new { scg, document })
            .FirstOrDefaultAsync(item => item.scg.Id == scgId && item.scg.UserId == userId && !item.scg.IsDeleted && !item.document.IsDeleted);

        if (scgRecord is null)
        {
            throw new AppException("SCG 不存在", 404);
        }

        var graph = JsonSerializer.Deserialize<ScgGraphDto>(scgRecord.scg.ScgJson) ?? new ScgGraphDto();
        ValidateGraph(graph);

        var now = SystemTime.Now();
        scgRecord.scg.IsConfirmed = true;
        scgRecord.scg.ConfirmedAt = now;
        scgRecord.scg.UpdatedAt = now;
        scgRecord.document.ProcessStatus = DocumentProcessStatus.ScgConfirmed;
        scgRecord.document.UpdatedAt = now;
        await _dbContext.SaveChangesAsync();

        return MapDetail(scgRecord.scg, scgRecord.document.DocumentName, ParseDocumentIds(scgRecord.scg.DocumentIdsKey, scgRecord.scg.DocumentId), graph);
    }

    public async Task DeleteAsync(long userId, long scgId)
    {
        await EnsureScgNameColumnReadyAsync();

        var scgRecord = await _dbContext.ScgRecords.FirstOrDefaultAsync(item => item.Id == scgId && item.UserId == userId && !item.IsDeleted);
        if (scgRecord is null)
        {
            throw new AppException("SCG 不存在", 404);
        }

        var scgHistoryRecords = await _dbContext.ScgHistoryRecords.Where(item => item.ScgRecordId == scgId && item.UserId == userId).ToListAsync();
        if (scgHistoryRecords.Count > 0)
        {
            _dbContext.ScgHistoryRecords.RemoveRange(scgHistoryRecords);
        }

        var mrRecords = await _dbContext.MrRecords.Where(item => item.ScgRecordId == scgId && item.UserId == userId && !item.IsDeleted).ToListAsync();
        if (mrRecords.Count > 0)
        {
            var mrRecordIds = mrRecords.Select(item => item.Id).ToList();
            var historyRecords = await _dbContext.MrHistoryRecords.Where(item => item.UserId == userId && mrRecordIds.Contains(item.MrRecordId)).ToListAsync();
            _dbContext.MrHistoryRecords.RemoveRange(historyRecords);
            _dbContext.MrRecords.RemoveRange(mrRecords);
        }

        _dbContext.ScgRecords.Remove(scgRecord);
        await _dbContext.SaveChangesAsync();
    }

    private async Task AddHistorySnapshotAsync(long scgRecordId, long userId, string scgName, string operationType, string scgJson)
    {
        _dbContext.ScgHistoryRecords.Add(new ScgHistoryRecord
        {
            ScgRecordId = scgRecordId,
            UserId = userId,
            ScgName = scgName,
            OperationType = operationType,
            ScgJson = scgJson,
            CreatedAt = SystemTime.Now(),
            UpdatedAt = SystemTime.Now()
        });
        await _dbContext.SaveChangesAsync();
    }

    private async Task EnsureScgNameColumnReadyAsync()
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT COUNT(*)
FROM information_schema.columns
WHERE table_schema = DATABASE()
  AND table_name = @tableName
  AND column_name = @columnName";

            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "@tableName";
            tableParam.Value = "scg_records";
            command.Parameters.Add(tableParam);

            var columnParam = command.CreateParameter();
            columnParam.ParameterName = "@columnName";
            columnParam.Value = "scg_name";
            command.Parameters.Add(columnParam);

            var result = await command.ExecuteScalarAsync();
            var count = result is null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            if (count == 0)
            {
                await _dbContext.Database.ExecuteSqlRawAsync("ALTER TABLE `scg_records` ADD COLUMN `scg_name` varchar(255) CHARACTER SET utf8mb4 NOT NULL DEFAULT '' AFTER `user_id`;");
            }

            await _dbContext.Database.ExecuteSqlRawAsync("UPDATE `scg_records` SET `scg_name` = CONCAT(SUBSTRING_INDEX(SUBSTRING_INDEX(`document_names_summary`, ' / ', 1), '.', 1), '_SCG_V01') WHERE `scg_name` = '' OR `scg_name` IS NULL;");
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<int> GetNextScgVersionAsync(long userId, string documentIdsKey)
    {
        var count = await _dbContext.ScgRecords.CountAsync(item => item.UserId == userId && !item.IsDeleted && item.DocumentIdsKey == documentIdsKey);
        return count + 1;
    }

    private static string BuildScgName(List<Document> documents, int version)
    {
        var firstDocument = documents.First();
        var shortName = SimplifyFileName(firstDocument.OriginalFileName);
        var versionTag = $"V{version:00}";
        if (documents.Count == 1)
        {
            return $"{shortName}_SCG_{versionTag}";
        }

        return $"{shortName}_SCG({documents.Count})_{versionTag}";
    }

    private static string SimplifyFileName(string originalFileName)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName)?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
        {
            return "未命名文件";
        }

        var builder = new StringBuilder();
        foreach (var currentChar in fileNameWithoutExtension)
        {
            if (Path.GetInvalidFileNameChars().Contains(currentChar))
            {
                continue;
            }

            builder.Append(currentChar);
        }

        var normalized = Regex.Replace(builder.ToString(), "\\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "未命名文件" : normalized;
    }

    private static void ValidateGraph(ScgGraphDto graph)
    {
        if (graph.Nodes is null || graph.Edges is null)
        {
            throw new AppException("SCG 图结构不完整");
        }

        if (graph.Nodes.Count == 0)
        {
            throw new AppException("SCG 节点不能为空");
        }

        var nodeTypes = new HashSet<string> { "input", "constraint", "output" };
        var edgeTypes = new HashSet<string> { "define", "causal", "condition" };
        var nodeIds = new HashSet<string>();
        var edgeIds = new HashSet<string>();

        foreach (var node in graph.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id) || string.IsNullOrWhiteSpace(node.Label) || !nodeTypes.Contains(node.Type))
            {
                throw new AppException("SCG 节点结构不合法");
            }

            if (!nodeIds.Add(node.Id))
            {
                throw new AppException("SCG 节点 ID 不能重复");
            }
        }

        foreach (var edge in graph.Edges)
        {
            if (string.IsNullOrWhiteSpace(edge.Id) || !edgeTypes.Contains(edge.Type) || !nodeIds.Contains(edge.SourceNodeId) || !nodeIds.Contains(edge.TargetNodeId))
            {
                throw new AppException("SCG 边结构不合法");
            }

            if (!edgeIds.Add(edge.Id))
            {
                throw new AppException("SCG 边 ID 不能重复");
            }

            if (edge.SourceNodeId == edge.TargetNodeId)
            {
                throw new AppException("SCG 边不允许自连接");
            }
        }
    }

    private static List<long> NormalizeDocumentIds(List<long> documentIds)
    {
        return documentIds
            .Where(item => item > 0)
            .Distinct()
            .OrderBy(item => item)
            .ToList();
    }

    private static string BuildDocumentIdsKey(List<long> documentIds)
    {
        return string.Join(",", documentIds);
    }

    private static List<long> ParseDocumentIds(string documentIdsKey, long fallbackDocumentId)
    {
        if (string.IsNullOrWhiteSpace(documentIdsKey))
        {
            return [fallbackDocumentId];
        }

        return documentIdsKey
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => long.TryParse(item, out var id) ? id : 0)
            .Where(item => item > 0)
            .ToList();
    }

    private static string GetPhysicalPath(string relativeFilePath)
    {
        var physicalPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", relativeFilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        return Path.GetFullPath(physicalPath);
    }

    private static ScgDetailResponse MapDetail(ScgRecord record, string documentName, List<long> documentIds, ScgGraphDto graph)
    {
        return new ScgDetailResponse
        {
            Id = record.Id,
            DocumentId = record.DocumentId,
            DocumentIds = documentIds,
            ScgName = record.ScgName,
            DocumentName = documentName,
            DocumentNamesSummary = string.IsNullOrWhiteSpace(record.DocumentNamesSummary) ? documentName : record.DocumentNamesSummary,
            ScgGraph = graph,
            IsConfirmed = record.IsConfirmed,
            ConfirmedAt = record.ConfirmedAt,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }
}






