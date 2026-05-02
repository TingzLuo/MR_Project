namespace MRProject.Api.Services.Llm;

public static class MrPromptTemplate
{
    public static string Build(string documentNamesSummary, string scgJson)
    {
        return
            "你是一个软件测试蜕变关系生成助手，请基于给定的 SCG 生成蜕变关系列表。\n\n" +
            "要求：\n" +
            "1. 仅返回 JSON 数组\n" +
            "2. 每条 MR 必须包含字段：id、inputRelation、outputRelation、description\n" +
            "3. 所有字段值都必须是字符串，不能返回对象、数组、数字或布尔值\n" +
            "4. inputRelation 用于描述输入之间的变化规则，不是具体输入样本\n" +
            "5. outputRelation 用于描述输入关系成立时输出之间应满足的关系约束，不是单个期望值\n" +
            "6. description 用于说明该条 MR 的测试意图、适用场景或补充说明\n" +
            "7. 不要输出 Markdown，不要输出解释文字\n\n" +
            "示例：\n" +
            "[\n" +
            "  {\n" +
            "    \"id\": \"mr1\",\n" +
            "    \"inputRelation\": \"在其他条件不变时，将输入参数按比例放大 2 倍\",\n" +
            "    \"outputRelation\": \"输出结果应保持单调不减或按相同比例变化\",\n" +
            "    \"description\": \"用于验证系统在输入缩放场景下的输出关系一致性\"\n" +
            "  }\n" +
            "]\n\n" +
            $"文件摘要：{documentNamesSummary}\n\n" +
            "SCG JSON：\n" +
            scgJson;
    }
}
