namespace MRProject.Api.DTOs.Admin;

public class AdminUserListItemResponse
{
    public long Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string RealName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
