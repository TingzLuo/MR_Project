namespace MRProject.Api.Services.Llm;

public static class ScgPromptTemplate
{
    public static string Build(string documentName, string sourceText)
    {
        return
            "你是一个软件测试分析助手，请基于给定文档内容生成 SCG（语义约束图）。\n\n" +
            "要求：\n" +
            "1. 节点类型只能是 input、constraint、output\n" +
            "2. 边类型只能是 define、causal、condition\n" +
            "3. 返回标准 JSON，对象格式为：\n" +
            "{\n" +
            "  \"nodes\": [{ \"id\": \"n1\", \"type\": \"input\", \"label\": \"...\", \"x\": 120, \"y\": 120 }],\n" +
            "  \"edges\": [{ \"id\": \"e1\", \"sourceNodeId\": \"n1\", \"targetNodeId\": \"n2\", \"type\": \"define\" }]\n" +
            "}\n" +
            "4. 节点至少包含一个 input、一个 constraint、一个 output\n" +
            "5. 坐标应适合前端图展示\n\n" +
            $"文档名称：{documentName}\n\n" +
            "文档内容：\n" +
            sourceText;
    }
}
