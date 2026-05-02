namespace MRProject.Api.Services.Interfaces;

using MRProject.Api.DTOs.Scg;
using MRProject.Api.DTOs.Mr;

public interface ILlmService
{
    Task<ScgGraphDto> GenerateScgAsync(string prompt, string sourceText, string documentName);

    Task<List<MrItemDto>> GenerateMrAsync(string prompt, string scgJson, string documentNamesSummary);
}
