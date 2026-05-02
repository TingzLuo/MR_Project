namespace MRProject.Api.DTOs.Admin;

public class AdminUserQueryRequest
{
    public string? Keyword { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
