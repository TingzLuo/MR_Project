namespace MRProject.Api.DTOs.Documents;

public class DocumentDetailResponse
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string DocumentName { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public string ProcessStatus { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
