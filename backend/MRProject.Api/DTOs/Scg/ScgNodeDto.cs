namespace MRProject.Api.DTOs.Scg;

public class ScgNodeDto
{
    public string Id { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string? Detail { get; set; }

    public int X { get; set; }

    public int Y { get; set; }
}

