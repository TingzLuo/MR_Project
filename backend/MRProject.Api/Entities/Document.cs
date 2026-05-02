using MRProject.Api.Enums;

namespace MRProject.Api.Entities;

public class Document : BaseEntity
{
    public long UserId { get; set; }

    public string DocumentName { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DocumentProcessStatus ProcessStatus { get; set; }

    public bool IsDeleted { get; set; }

    public User? User { get; set; }
}
