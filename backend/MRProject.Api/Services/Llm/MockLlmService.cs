using MRProject.Api.DTOs.Scg;
using MRProject.Api.DTOs.Mr;
using MRProject.Api.Services.Interfaces;
using System.Text.Json;

namespace MRProject.Api.Services.Llm;

public class MockLlmService : ILlmService
{
    public Task<ScgGraphDto> GenerateScgAsync(string prompt, string sourceText, string documentName)
    {
        var keywords = SplitKeywords(sourceText);
        var inputLabel = keywords.ElementAtOrDefault(0) ?? $"{documentName} 输入";
        var constraintLabel = keywords.ElementAtOrDefault(1) ?? $"{documentName} 约束";
        var outputLabel = keywords.ElementAtOrDefault(2) ?? $"{documentName} 输出";

        var graph = new ScgGraphDto
        {
            Nodes =
            [
                new ScgNodeDto { Id = "n1", Type = "input", Label = inputLabel, X = 120, Y = 140 },
                new ScgNodeDto { Id = "n2", Type = "constraint", Label = constraintLabel, X = 360, Y = 140 },
                new ScgNodeDto { Id = "n3", Type = "output", Label = outputLabel, X = 620, Y = 140 }
            ],
            Edges =
            [
                new ScgEdgeDto { Id = "e1", SourceNodeId = "n1", TargetNodeId = "n2", Type = "define" },
                new ScgEdgeDto { Id = "e2", SourceNodeId = "n2", TargetNodeId = "n3", Type = "causal" }
            ]
        };

        if (keywords.Count >= 4)
        {
            graph.Nodes.Add(new ScgNodeDto { Id = "n4", Type = "constraint", Label = keywords[3], X = 360, Y = 300 });
            graph.Edges.Add(new ScgEdgeDto { Id = "e3", SourceNodeId = "n1", TargetNodeId = "n4", Type = "condition" });
            graph.Edges.Add(new ScgEdgeDto { Id = "e4", SourceNodeId = "n4", TargetNodeId = "n3", Type = "define" });
        }

        return Task.FromResult(graph);
    }

    public Task<List<MrItemDto>> GenerateMrAsync(string prompt, string scgJson, string documentNamesSummary)
    {
        var graph = JsonSerializer.Deserialize<ScgGraphDto>(scgJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ScgGraphDto();
        var inputNode = graph.Nodes.FirstOrDefault(item => item.Type == "input")?.Label ?? "输入条件";
        var outputNode = graph.Nodes.FirstOrDefault(item => item.Type == "output")?.Label ?? "输出结果";
        var constraintNode = graph.Nodes.FirstOrDefault(item => item.Type == "constraint")?.Label ?? "约束条件";

        var mrItems = new List<MrItemDto>
        {
            new MrItemDto
            {
                Id = "mr1",
                InputRelation = $"在满足 {constraintNode} 前提下增强 {inputNode}",
                OutputRelation = $"{outputNode} 应保持有效或呈现可预测增强关系",
                Description = $"基于 SCG 中 {inputNode} 到 {outputNode} 的因果链生成，用于验证输入增强场景下的输出关系约束"
            },
            new MrItemDto
            {
                Id = "mr2",
                InputRelation = $"构造接近 {constraintNode} 边界的输入变化",
                OutputRelation = $"{outputNode} 应呈现与边界变化一致的输出关系约束",
                Description = $"基于约束节点 {constraintNode} 与输出节点 {outputNode} 的关系生成，用于验证边界条件下的蜕变关系"
            }
        };

        return Task.FromResult(mrItems);
    }

    private static List<string> SplitKeywords(string sourceText)
    {
        return sourceText.Replace("\r", " ").Replace("\n", " ")
            .Split(new[] { ' ', '，', '。', ',', ';', '；', ':', '：' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(item => item.Length >= 2)
            .Distinct()
            .Take(6)
            .ToList();
    }
}
