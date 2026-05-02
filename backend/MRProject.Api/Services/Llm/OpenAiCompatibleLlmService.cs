using MRProject.Api.Common;
using MRProject.Api.DTOs.Scg;
using MRProject.Api.DTOs.Mr;
using MRProject.Api.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MRProject.Api.Services.Llm;

public class OpenAiCompatibleLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly LlmOptions _llmOptions;

    public OpenAiCompatibleLlmService(HttpClient httpClient, IOptions<LlmOptions> llmOptions)
    {
        _httpClient = httpClient;
        _llmOptions = llmOptions.Value;

        if (string.IsNullOrWhiteSpace(_llmOptions.BaseUrl))
        {
            throw new InvalidOperationException("Llm:BaseUrl 未配置");
        }

        if (string.IsNullOrWhiteSpace(_llmOptions.ApiKey))
        {
            throw new InvalidOperationException("Llm:ApiKey 未配置");
        }

        _httpClient.BaseAddress = new Uri(_llmOptions.BaseUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(_llmOptions.TimeoutSeconds <= 0 ? 120 : _llmOptions.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _llmOptions.ApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public Task<ScgGraphDto> GenerateScgAsync(string prompt, string sourceText, string documentName)
    {
        return SendAndParseAsync(prompt, content =>
            JsonSerializer.Deserialize<ScgGraphDto>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new AppException("大模型返回的 SCG JSON 为空", 500));
    }

    public Task<List<MrItemDto>> GenerateMrAsync(string prompt, string scgJson, string documentNamesSummary)
    {
        return SendAndParseAsync(prompt, ParseMrItems);
    }

    private async Task<T> SendAndParseAsync<T>(string prompt, Func<string, T> parser)
    {
        var safePrompt = NormalizePrompt(prompt);
        var requestBody = new
        {
            model = _llmOptions.ModelName,
            messages = new object[]
            {
                new { role = "system", content = "你是一个严谨的软件测试分析助手。你必须只返回合法 JSON，不要输出 Markdown 代码块，不要输出解释。" },
                new { role = "user", content = safePrompt }
            },
            temperature = 0.2
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

        using var response = await _httpClient.SendAsync(request);
        var responseText = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new AppException($"大模型调用失败：{response.StatusCode} {responseText}", 500);
        }

        using var jsonDocument = JsonDocument.Parse(responseText);
        if (!jsonDocument.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            throw new AppException("大模型返回内容缺少 choices", 500);
        }

        var content = choices[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        var cleaned = ExtractJson(content);

        try
        {
            return parser(cleaned);
        }
        catch (JsonException exception)
        {
            throw new AppException($"大模型返回的 JSON 无法解析：{exception.Message}。原始内容：{content}", 500);
        }
    }

    private static List<MrItemDto> ParseMrItems(string content)
    {
        using var document = JsonDocument.Parse(content);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new AppException("大模型返回的 MR 不是数组", 500);
        }

        var items = new List<MrItemDto>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            items.Add(new MrItemDto
            {
                Id = ReadAsString(item, "id"),
                InputRelation = ReadAsString(item, "inputRelation", "sourceInput"),
                OutputRelation = ReadAsString(item, "outputRelation", "expectedRelation"),
                Description = ReadAsString(item, "description")
            });
        }
        return items;
    }

    private static string ReadAsString(JsonElement element, string propertyName, string? fallbackPropertyName = null)
    {
        if (!element.TryGetProperty(propertyName, out var property) && (!string.IsNullOrWhiteSpace(fallbackPropertyName) && !element.TryGetProperty(fallbackPropertyName, out property)))
        {
            return string.Empty;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString() ?? string.Empty,
            JsonValueKind.Object => property.GetRawText(),
            JsonValueKind.Array => property.GetRawText(),
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => property.GetRawText()
        };
    }

    private static string NormalizePrompt(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;
        var builder = new StringBuilder(content.Length);
        foreach (var ch in content)
        {
            if (char.IsControl(ch) && ch != '\r' && ch != '\n' && ch != '\t') continue;
            builder.Append(ch);
        }
        var normalized = builder.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
        while (normalized.Contains("\n\n\n")) normalized = normalized.Replace("\n\n\n", "\n\n");
        if (normalized.Length > 12000) normalized = normalized[..12000];
        return normalized.Trim();
    }

    private static string ExtractJson(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```") && trimmed.EndsWith("```"))
        {
            var lines = trimmed.Split('\n').Select(line => line.TrimEnd('\r')).ToList();
            if (lines.Count >= 2)
            {
                lines.RemoveAt(0);
                lines.RemoveAt(lines.Count - 1);
                trimmed = string.Join("\n", lines).Trim();
                if (trimmed.StartsWith("json", StringComparison.OrdinalIgnoreCase)) trimmed = trimmed[4..].Trim();
            }
        }
        var startBrace = trimmed.IndexOf('{');
        var endBrace = trimmed.LastIndexOf('}');
        var startBracket = trimmed.IndexOf('[');
        var endBracket = trimmed.LastIndexOf(']');
        if (startBracket >= 0 && endBracket > startBracket && (startBrace == -1 || startBracket < startBrace)) return trimmed[startBracket..(endBracket + 1)];
        if (startBrace >= 0 && endBrace > startBrace) return trimmed[startBrace..(endBrace + 1)];
        return trimmed;
    }
}
