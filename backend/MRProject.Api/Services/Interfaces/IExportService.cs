namespace MRProject.Api.Services.Interfaces;

public interface IExportService
{
    Task<(byte[] Content, string FileName, string ContentType)> ExportScgAsync(long userId, long scgId);

    Task<(byte[] Content, string FileName, string ContentType)> ExportMrAsync(long userId, long mrId);

    Task<(byte[] Content, string FileName, string ContentType)> ExportUsersAsync();

    Task<(byte[] Content, string FileName, string ContentType)> ExportDocumentsAsync();

    Task<(byte[] Content, string FileName, string ContentType)> ExportScgRecordsAsync();

    Task<(byte[] Content, string FileName, string ContentType)> ExportMrRecordsAsync();

    Task<(byte[] Content, string FileName, string ContentType)> ExportSystemBackupAsync();
}
