using MRProject.Api.Common;
using MRProject.Api.Data;
using MRProject.Api.DTOs.Documents;
using MRProject.Api.Entities;
using MRProject.Api.Enums;
using MRProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MRProject.Api.Services;

public class DocumentService : IDocumentService
{
    private static readonly HashSet<string> AllowedExtensions = [".pdf", ".docx"];

    private static readonly Dictionary<string, string[]> AllowedContentTypes = new()
    {
        [".pdf"] = ["application/pdf"],
        [".docx"] =
        [
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/octet-stream"
        ]
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly FileStorageOptions _fileStorageOptions;
    private readonly IWebHostEnvironment _environment;

    public DocumentService(
        ApplicationDbContext dbContext,
        IOptions<FileStorageOptions> fileStorageOptions,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _fileStorageOptions = fileStorageOptions.Value;
        _environment = environment;
    }

    public async Task<UploadDocumentsResponse> UploadAsync(long userId, List<IFormFile> files)
    {
        if (files.Count == 0)
        {
            throw new AppException("请至少上传一个文件");
        }

        var uploadDirectory = GetUploadDirectory();
        Directory.CreateDirectory(uploadDirectory);

        var documents = new List<Document>();

        foreach (var file in files)
        {
            ValidateFile(file);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(uploadDirectory, storedFileName);

            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var now = SystemTime.Now();

            var document = new Document
            {
                UserId = userId,
                DocumentName = Path.GetFileNameWithoutExtension(file.FileName),
                OriginalFileName = file.FileName,
                StoredFileName = storedFileName,
                FilePath = Path.Combine(_fileStorageOptions.RootPath, storedFileName).Replace("\\", "/"),
                FileType = extension.TrimStart('.'),
                FileSize = file.Length,
                ProcessStatus = DocumentProcessStatus.Uploaded,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            };

            documents.Add(document);
            _dbContext.Documents.Add(document);
        }

        await _dbContext.SaveChangesAsync();

        return new UploadDocumentsResponse
        {
            UploadedItems = documents.Select(MapItem).ToList()
        };
    }

    public async Task<PagedResult<DocumentItemResponse>> GetPagedListAsync(long userId, DocumentQueryRequest request)
    {
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;
        var keyword = request.Keyword?.Trim() ?? string.Empty;

        var query = _dbContext.Documents
            .AsNoTracking()
            .Where(document => document.UserId == userId && !document.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(document =>
                document.DocumentName.Contains(keyword) ||
                document.OriginalFileName.Contains(keyword));
        }

        var total = await query.CountAsync();
        var documents = await query
            .OrderByDescending(document => document.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = documents.Select(MapItem).ToList();

        return new PagedResult<DocumentItemResponse>
        {
            Items = items,
            Total = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<DocumentDetailResponse> GetDetailAsync(long userId, long documentId)
    {
        var document = await _dbContext.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == documentId && item.UserId == userId && !item.IsDeleted);

        if (document is null)
        {
            throw new AppException("文件不存在", 404);
        }

        return new DocumentDetailResponse
        {
            Id = document.Id,
            UserId = document.UserId,
            DocumentName = document.DocumentName,
            OriginalFileName = document.OriginalFileName,
            StoredFileName = document.StoredFileName,
            FilePath = document.FilePath,
            FileType = document.FileType,
            FileSize = document.FileSize,
            ProcessStatus = MapStatus(document.ProcessStatus),
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt
        };
    }

    public async Task DeleteAsync(long userId, long documentId)
    {
        var document = await _dbContext.Documents
            .FirstOrDefaultAsync(item => item.Id == documentId && item.UserId == userId && !item.IsDeleted);

        if (document is null)
        {
            throw new AppException("文件不存在", 404);
        }

        document.IsDeleted = true;
        document.UpdatedAt = SystemTime.Now();

        var physicalPath = Path.Combine(_environment.ContentRootPath, document.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        await _dbContext.SaveChangesAsync();
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length <= 0)
        {
            throw new AppException("不允许上传空文件");
        }

        var maxFileSize = _fileStorageOptions.MaxFileSizeMb * 1024L * 1024L;
        if (file.Length > maxFileSize)
        {
            throw new AppException($"文件大小不能超过 {_fileStorageOptions.MaxFileSizeMb} MB");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new AppException("仅支持上传 docx 或 pdf 文件");
        }

        if (!AllowedContentTypes.TryGetValue(extension, out var contentTypes) || !contentTypes.Contains(file.ContentType))
        {
            throw new AppException("文件 MIME 类型不合法");
        }
    }

    private string GetUploadDirectory()
    {
        var relativeRoot = _fileStorageOptions.RootPath.Replace("/", Path.DirectorySeparatorChar.ToString());
        return Path.Combine(_environment.ContentRootPath, relativeRoot);
    }

    private static DocumentItemResponse MapItem(Document document)
    {
        return new DocumentItemResponse
        {
            Id = document.Id,
            DocumentName = document.DocumentName,
            OriginalFileName = document.OriginalFileName,
            FileType = document.FileType,
            FileSize = document.FileSize,
            ProcessStatus = MapStatus(document.ProcessStatus),
            CreatedAt = document.CreatedAt
        };
    }

    private static string MapStatus(DocumentProcessStatus status)
    {
        return status switch
        {
            DocumentProcessStatus.Uploaded => "uploaded",
            DocumentProcessStatus.Parsed => "parsed",
            DocumentProcessStatus.ScgGenerated => "scgGenerated",
            DocumentProcessStatus.ScgConfirmed => "scgConfirmed",
            DocumentProcessStatus.MrGenerated => "mrGenerated",
            DocumentProcessStatus.Archived => "archived",
            _ => "uploaded"
        };
    }
}
