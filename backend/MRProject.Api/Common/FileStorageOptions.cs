namespace MRProject.Api.Common;

public class FileStorageOptions
{
    public string RootPath { get; set; } = "storage/documents";

    public int MaxFileSizeMb { get; set; } = 20;
}
