namespace MRProject.Api.DTOs.Dashboard;

public class UserStatsResponse
{
    public int FileCount { get; set; }

    public int LlmCallCount { get; set; }

    public int ScgCount { get; set; }

    public int MrCount { get; set; }
}
