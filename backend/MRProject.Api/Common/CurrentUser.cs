using MRProject.Api.Enums;

namespace MRProject.Api.Common;

public class CurrentUser
{
    public long UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public UserRole Role { get; set; }
}
