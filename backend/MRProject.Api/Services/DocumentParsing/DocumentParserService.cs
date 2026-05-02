using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MRProject.Api.Common;
using MRProject.Api.Services.Interfaces;
using UglyToad.PdfPig;

namespace MRProject.Api.Services.DocumentParsing;

public class DocumentParserService : IDocumentParserService
{
    public Task<string> ParseAsync(string filePath, string fileType)
    {
        return fileType.ToLowerInvariant() switch
        {
            "docx" => Task.FromResult(ParseDocx(filePath)),
            "pdf" => Task.FromResult(ParsePdf(filePath)),
            _ => throw new AppException("暂不支持该文件类型解析")
        };
    }

    private static string ParseDocx(string filePath)
    {
        using var document = WordprocessingDocument.Open(filePath, false);
        var body = document.MainDocumentPart?.Document.Body;
        if (body is null)
        {
            throw new AppException("Word 文档内容为空");
        }

        var texts = body.Descendants<Text>()
            .Select(item => item.Text?.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item));

        var result = string.Join(Environment.NewLine, texts);
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new AppException("Word 文档解析结果为空");
        }

        return result;
    }

    private static string ParsePdf(string filePath)
    {
        using var document = PdfDocument.Open(filePath);
        var texts = document.GetPages()
            .Select(page => page.Text?.Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text));

        var result = string.Join(Environment.NewLine, texts);
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new AppException("PDF 文档解析结果为空");
        }

        return result;
    }
}
