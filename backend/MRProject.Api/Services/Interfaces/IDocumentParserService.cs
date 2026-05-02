using MRProject.Api.DTOs.Scg;

namespace MRProject.Api.Services.Interfaces;

public interface IDocumentParserService
{
    Task<string> ParseAsync(string filePath, string fileType);
}
