using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;

// ===== 中文控制台输入乱码修复（中文 Windows 控制台代码页 936 / GBK）=====
// Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
// Console.InputEncoding = Encoding.GetEncoding(936);

// ---------- 配置 ----------
IConfigurationRoot config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string? model = config["DashScopeModel"];
string? key = config["DashScopeKey"];

// 阿里云百炼（DashScope）兼容 OpenAI 接口
OpenAIClientOptions clientOptions = new()
{
    Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1")
};

IChatClient client =
    new OpenAIClient(new ApiKeyCredential(key!), clientOptions)
        .GetChatClient(model)
        .AsIChatClient();

// OpenAI 官方接口的用户改用：
// IChatClient client =
//     new OpenAIClient(key!)
//         .GetChatClient(model)
//         .AsIChatClient();

// ---------- 固定的测试评论（含正负面，便于观察情感判断）----------
string review = """
    收到货第三天了，物流确实快，包装也很结实没破损。
    但是用了几天发现续航比宣传的少很多，标称10小时实际只有6小时左右。
    联系客服说是正常波动，态度倒是可以但没给解决方案。综合来看一般吧。
    """;


/*
// # 8. 实验一：模糊 Prompt 基线（故意踩坑）
// ---------- V1：模糊基线 ----------
string promptV1 = $"分析这条评论：{review}";

Console.WriteLine("===== V1 模糊基线 =====");
Console.WriteLine($"Prompt：{promptV1}");
Console.WriteLine();

ChatResponse<ReviewAnalysis> responseV1 =
    await client.GetResponseAsync<ReviewAnalysis>(
        promptV1,
        new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 1300 });

if (responseV1.TryGetResult(out ReviewAnalysis? r1))
{
    PrintResult(r1);
}
else
{
    Console.WriteLine($"解析失败，原始返回：{responseV1.Text}");
}
*/

/*
// # 9. 实验二：加指令 + 输出格式（Clear & Specific）
string promptV2 = $"""
    请分析下面这条产品评论，以 JSON 格式输出，输出结果必须用中文：
    - sentiment：情感倾向（Positive/Negative/Neutral）
    - score：0-10 情感评分
    - keywords：3个关键词
    - conclusion：一句话结论

    评论：{review}
    """;

Console.WriteLine("===== V2 指令+格式 =====");
Console.WriteLine();

ChatResponse<ReviewAnalysis> responseV2 =
    await client.GetResponseAsync<ReviewAnalysis>(
        promptV2,
        new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 1300 });

if (responseV2.TryGetResult(out ReviewAnalysis? r2))
    PrintResult(r2);
else
    Console.WriteLine($"解析失败：{responseV2.Text}");
*/

/*
// # 10. 实验三：加角色设定（Role / Persona）
// ---------- V3：加角色 ----------
string promptV3 = $"""
    你是一位专业的客户体验分析师，擅长从评论中提取洞察。
    请分析下面这条产品评论，以 JSON 格式输出：
    - sentiment：情感倾向（Positive/Negative/Neutral）
    - score：0-10 情感评分
    - keywords：3个关键词
    - conclusion：一句话结论

    评论：{review}
    """;

Console.WriteLine("===== V3 加角色 =====");
Console.WriteLine();

ChatResponse<ReviewAnalysis> responseV3 =
    await client.GetResponseAsync<ReviewAnalysis>(
        promptV3,
        new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 1300 });

if (responseV3.TryGetResult(out ReviewAnalysis? r3))
    PrintResult(r3);
else
    Console.WriteLine($"解析失败：{responseV3.Text}");
*/


/*
// # 11. 实验四：加分步推理（Step-by-step / Chain of Thought）

// 把“分析”这个笼统动作，拆成模型容易遵循的步骤：

// ---------- V4：分步推理 ----------
string promptV4 = $"""
    你是一位专业的客户体验分析师。请按以下步骤分析评论：
    1. 先识别整体情感倾向（综合考虑正面与负面因素，以核心痛点为准）
    2. 根据情感强度给出0-10评分
    3. 提取3个最能代表评论主题的关键词
    4. 用一句话总结结论

    以 JSON 格式输出：sentiment、score、keywords、conclusion。

    评论：{review}
    """;

Console.WriteLine("===== V4 加分步 =====");
Console.WriteLine();

ChatResponse<ReviewAnalysis> responseV4 =
    await client.GetResponseAsync<ReviewAnalysis>(
        promptV4,
        new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 1400 });

if (responseV4.TryGetResult(out ReviewAnalysis? r4))
    PrintResult(r4);
else
    Console.WriteLine($"解析失败：{responseV4.Text}");
*/


/*
// # 12. 实验五：加约束与边界（Constraints）

// V4 已经不错，但评分标准、关键词词性、结论字数仍可能漂移。加硬约束：

// ---------- V5：加约束 ----------
string promptV5 = $"""
    你是一位专业的客户体验分析师。请按以下步骤分析评论：
    1. 先识别整体情感倾向（综合考虑正面与负面因素，以核心痛点为准）
    2. 根据情感强度给出0-10评分（评分标准：10=极度满意，7=基本满意，
       5=中性/一般，3=不满意，1=极度不满）
    3. 提取3个最能代表评论主题的关键词（必须是名词或名词短语，不要动词）
    4. 用一句话总结结论（不超过20个中文字，只陈述结论不加建议）

    只输出JSON，不要任何解释。字段：sentiment、score、keywords、conclusion。

    评论：{review}
    """;

Console.WriteLine("===== V5 加约束 =====");
Console.WriteLine();

ChatResponse<ReviewAnalysis> responseV5 =
    await client.GetResponseAsync<ReviewAnalysis>(
        promptV5,
        new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 2400 });

if (responseV5.TryGetResult(out ReviewAnalysis? r5))
    PrintResult(r5);
else
    Console.WriteLine($"解析失败：{responseV5.Text}");
*/


/*
// # 13. 实验六：加分隔符（Delimiters）

// 最后一个技巧：用分隔符把“指令”和“输入数据”物理隔离。

// ---------- V6：加分隔符 ----------
string promptV6 = $"""
    你是一位专业的客户体验分析师。请按以下步骤分析评论：
    1. 先识别整体情感倾向（综合考虑正面与负面因素，以核心痛点为准）
    2. 根据情感强度给出0-10评分（评分标准：10=极度满意，7=基本满意，
       5=中性/一般，3=不满意，1=极度不满）
    3. 提取3个最能代表评论主题的关键词（必须是名词或名词短语，不要动词）
    4. 用一句话总结结论（不超过20个中文字，只陈述结论不加建议）

    只输出JSON，不要任何解释。字段：sentiment、score、keywords、conclusion。

    <review>
    {review}
    </review>
    """;

Console.WriteLine("===== V6 加分隔符 =====");
Console.WriteLine();

ChatResponse<ReviewAnalysis> responseV6 =
    await client.GetResponseAsync<ReviewAnalysis>(
        promptV6,
        new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 1400 });

if (responseV6.TryGetResult(out ReviewAnalysis? r6))
    PrintResult(r6);
else
    Console.WriteLine($"解析失败：{responseV6.Text}");
*/

/*
// ## 14.1 反模式 A：过度指令（Over-specified）
string badA = $"""
    你是一位有10年经验的客户体验分析师，曾在多家知名电商公司工作，
    擅长分析3C数码、家电、快消品评论。你精通情感分析、NLP、数据挖掘。
    你的分析风格是客观中立、数据驱动、注重细节。
    你拥有心理学硕士学位，理解消费者行为。
    请用最专业的态度分析下面这条评论，
    要深入挖掘用户潜在需求、情感动机、购买决策因素。
    分析维度包括但不限于：情感、评分、关键词、结论、潜在需求、
    改进建议、竞品对比、用户画像、复购可能性、NPS推荐值。
    以JSON格式输出以上所有维度。

    评论：{review}
    """;

Console.WriteLine("===== 反模式 A：过度指令 =====");
ChatResponse<ReviewAnalysis> respA =
    await client.GetResponseAsync<ReviewAnalysis>(
        badA, new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 4400 });
if (respA.TryGetResult(out ReviewAnalysis? ra))
    PrintResult(ra);
else
    Console.WriteLine($"解析失败（意料之中）：{respA.Text[..Math.Min(120, respA.Text.Length)]}…");
*/

/*
// ## 14.2 反模式 B：矛盾指令（Contradictory）
string badB = $"""
    请客观分析下面评论，给出中立的结论。
    同时请站在商家立场，尽量说好话，给出正面结论。
    评分要严格，但也要给高分鼓励商家。
    以JSON输出：sentiment、score、keywords、conclusion。

    评论：{review}
    """;

Console.WriteLine("===== 反模式 B：矛盾指令 =====");
ChatResponse<ReviewAnalysis> respB =
    await client.GetResponseAsync<ReviewAnalysis>(
        badB, new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 3300 });
if (respB.TryGetResult(out ReviewAnalysis? rb))
    PrintResult(rb);
else
    Console.WriteLine($"解析失败：{respB.Text}");
*/

// ## 14.3 反模式 C：缺乏输出约束（No Output Constraint）

string badC = $"你是分析师，请好好分析这条评论：{review}";

Console.WriteLine("===== 反模式 C：无输出约束 =====");
// 这次故意用普通 GetResponseAsync 看自由文本
ChatResponse respC = await client.GetResponseAsync(
    badC, new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 1300 });
Console.WriteLine($"自由文本返回：{respC.Text}");
Console.WriteLine("(没有字段、没有结构、程序无法直接用)");


// ---------- 工具函数 ----------
static void PrintResult(ReviewAnalysis r)
{
    Console.WriteLine($"  情感：{r.Sentiment}");
    Console.WriteLine($"  评分：{r.Score}/10");
    Console.WriteLine($"  关键词：{string.Join("、", r.Keywords ?? [])}");
    Console.WriteLine($"  结论：{r.Conclusion}");
    Console.WriteLine($"  完整度：{ScoreCompleteness(r)}/4");
}


static int ScoreCompleteness(ReviewAnalysis r)
{
    int filled = 0;
    if (r.Sentiment != default) filled++;
    if (r.Score is >= 0 and <= 10) filled++;
    if (r.Keywords is { Count: > 0 }) filled++;
    if (!string.IsNullOrWhiteSpace(r.Conclusion)) filled++;
    return filled;
}

// ---------- 输出契约（top-level 语句之后声明类型）----------
public enum Sentiment { Positive, Negative, Neutral }

public class ReviewAnalysis
{
    [Description("情感倾向，Positive/Negative/Neutral 之一")]
    public Sentiment Sentiment { get; set; }

    [Description("0-10 情感评分，10=极度满意，1=极度不满")]
    public int Score { get; set; }

    [Description("3个关键词")]
    public List<string>? Keywords { get; set; }

    [Description("一句话结论，不超过20字")]
    public string? Conclusion { get; set; }
}