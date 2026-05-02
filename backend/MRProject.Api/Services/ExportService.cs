using MRProject.Api.Common;
using MRProject.Api.Data;
using MRProject.Api.DTOs.Mr;
using MRProject.Api.DTOs.Scg;
using MRProject.Api.Services.Interfaces;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace MRProject.Api.Services;

public class ExportService : IExportService
{
    private readonly ApplicationDbContext _dbContext;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public ExportService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(byte[] Content, string FileName, string ContentType)> ExportScgAsync(long userId, long scgId)
    {
        var record = await _dbContext.ScgRecords.AsNoTracking().FirstOrDefaultAsync(item => item.Id == scgId && item.UserId == userId && !item.IsDeleted);
        if (record is null) throw new AppException("SCG 不存在", 404);
        var graph = JsonSerializer.Deserialize<ScgGraphDto>(record.ScgJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ScgGraphDto();
        var payload = new
        {
            record.Id,
            record.DocumentId,
            record.DocumentIdsKey,
            record.DocumentNamesSummary,
            record.IsConfirmed,
            record.ConfirmedAt,
            graph,
            record.CreatedAt,
            record.UpdatedAt
        };
        return CreateJsonFile(payload, $"scg_{scgId}_{DateTime.Now:yyyyMMddHHmmss}.json");
    }

    public async Task<(byte[] Content, string FileName, string ContentType)> ExportMrAsync(long userId, long mrId)
    {
        var record = await _dbContext.MrRecords.AsNoTracking().FirstOrDefaultAsync(item => item.Id == mrId && item.UserId == userId && !item.IsDeleted);
        if (record is null) throw new AppException("蜕变关系不存在", 404);
        var items = JsonSerializer.Deserialize<List<MrItemDto>>(record.MrJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        var headers = new List<string> { "MR ID", "输入关系", "输出关系", "描述", "生成时间" };
        var rows = items.Select(item => new List<string>
        {
            item.Id,
            item.InputRelation,
            item.OutputRelation,
            item.Description,
            record.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        }).ToList();

        return CreateExcelFile(headers, rows, $"mr_{mrId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx", "MR数据");
    }

    public async Task<(byte[] Content, string FileName, string ContentType)> ExportUsersAsync()
    {
        var users = await _dbContext.Users.AsNoTracking().Where(item => !item.IsDeleted).OrderBy(item => item.Id).ToListAsync();
        var headers = new List<string> { "账号ID", "用户名", "手机号", "邮箱号", "角色", "最后登录" };
        var rows = users.Select(item => new List<string>
        {
            item.Id.ToString(),
            item.Username,
            string.IsNullOrWhiteSpace(item.Phone) ? "未绑定" : item.Phone,
            item.Email,
            item.Role == Enums.UserRole.Admin ? "管理员" : "普通用户",
            item.LastLoginAt?.ToString("yyyy/MM/dd HH:mm:ss") ?? "从未登录"
        }).ToList();
        return CreateExcelFile(headers, rows, $"users_{DateTime.Now:yyyyMMddHHmmss}.xlsx", "用户数据");
    }

    public async Task<(byte[] Content, string FileName, string ContentType)> ExportDocumentsAsync()
    {
        var documents = await _dbContext.Documents.AsNoTracking().Where(item => !item.IsDeleted).OrderBy(item => item.Id).ToListAsync();
        var headers = new List<string> { "文档名称", "原始文件名", "类型", "文件大小", "状态", "上传时间" };
        var rows = documents.Select(item => new List<string>
        {
            item.DocumentName,
            item.OriginalFileName,
            item.FileType,
            item.FileSize.ToString(),
            item.ProcessStatus.ToString(),
            item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        }).ToList();
        return CreateExcelFile(headers, rows, $"documents_{DateTime.Now:yyyyMMddHHmmss}.xlsx", "文档数据");
    }

    public async Task<(byte[] Content, string FileName, string ContentType)> ExportScgRecordsAsync()
    {
        var records = await _dbContext.ScgRecords.AsNoTracking().Where(item => !item.IsDeleted).OrderBy(item => item.Id).ToListAsync();
        return CreateJsonFile(records, $"scg_records_{DateTime.Now:yyyyMMddHHmmss}.json");
    }

    public async Task<(byte[] Content, string FileName, string ContentType)> ExportMrRecordsAsync()
    {
        var records = await _dbContext.MrRecords.AsNoTracking().Where(item => !item.IsDeleted).OrderBy(item => item.Id).ToListAsync();
        var allItems = new List<(long RecordId, string DocumentNamesSummary, MrItemDto Item, DateTime UpdatedAt)>();
        foreach (var record in records)
        {
            var items = JsonSerializer.Deserialize<List<MrItemDto>>(record.MrJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            allItems.AddRange(items.Select(item => (record.Id, record.DocumentNamesSummary, item, record.UpdatedAt)));
        }

        var headers = new List<string> { "记录ID", "文件摘要", "MR ID", "输入关系", "输出关系", "描述", "生成时间" };
        var rows = allItems.Select(item => new List<string>
        {
            item.RecordId.ToString(),
            item.DocumentNamesSummary,
            item.Item.Id,
            item.Item.InputRelation,
            item.Item.OutputRelation,
            item.Item.Description,
            item.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        }).ToList();
        return CreateExcelFile(headers, rows, $"mr_records_{DateTime.Now:yyyyMMddHHmmss}.xlsx", "MR数据");
    }

    public async Task<(byte[] Content, string FileName, string ContentType)> ExportSystemBackupAsync()
    {
        var users = await _dbContext.Users.AsNoTracking().Where(item => !item.IsDeleted).ToListAsync();
        var documents = await _dbContext.Documents.AsNoTracking().Where(item => !item.IsDeleted).ToListAsync();
        var scgRecords = await _dbContext.ScgRecords.AsNoTracking().Where(item => !item.IsDeleted).ToListAsync();
        var mrRecords = await _dbContext.MrRecords.AsNoTracking().Where(item => !item.IsDeleted).ToListAsync();

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            AddZipEntry(archive, "users.json", JsonSerializer.Serialize(users, JsonOptions));
            AddZipEntry(archive, "documents.json", JsonSerializer.Serialize(documents, JsonOptions));
            AddZipEntry(archive, "scg_records.json", JsonSerializer.Serialize(scgRecords, JsonOptions));
            AddZipEntry(archive, "mr_records.json", JsonSerializer.Serialize(mrRecords, JsonOptions));
        }
        return (memoryStream.ToArray(), $"system_backup_{DateTime.Now:yyyyMMddHHmmss}.zip", "application/zip");
    }

    private static (byte[] Content, string FileName, string ContentType) CreateJsonFile(object data, string fileName)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        return (Encoding.UTF8.GetBytes(json), fileName, "application/json");
    }

    private static (byte[] Content, string FileName, string ContentType) CreateExcelFile(List<string> headers, List<List<string>> rows, string fileName, string sheetName)
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            var sheet = new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = sheetName
            };
            sheets.Append(sheet);

            var headerRow = new Row();
            foreach (var header in headers)
            {
                headerRow.Append(CreateTextCell(header));
            }
            sheetData.Append(headerRow);

            foreach (var row in rows)
            {
                var dataRow = new Row();
                foreach (var cell in row)
                {
                    dataRow.Append(CreateTextCell(cell));
                }
                sheetData.Append(dataRow);
            }

            workbookPart.Workbook.Save();
        }

        return (stream.ToArray(), fileName, ExcelContentType);
    }

    private static Cell CreateTextCell(string value)
    {
        return new Cell
        {
            DataType = CellValues.InlineString,
            InlineString = new InlineString(new Text(value ?? string.Empty))
        };
    }

    private static void AddZipEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write(content);
    }
}
