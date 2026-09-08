using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;

// ===== 中文控制台输入乱码修复（中文 Windows 控制台代码页 936 / GBK）=====
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.InputEncoding = Encoding.GetEncoding(936);

// ---------- 配置 ----------
IConfigurationRoot config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string? model = config["DashScopeModel"];
string? key = config["DashScopeKey"];

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

// ---------- 待分析的目标评论 ----------
string targetReview = """
    收到货第三天了，物流确实快，包装也很结实没破损。
    但是用了几天发现续航比宣传的少很多，标称10小时实际只有6小时左右。
    联系客服说是正常波动，态度倒是可以但没给解决方案。综合来看一般吧。
    """;

/*
// # 8. 实验一：Zero-shot 基线（复用 Day007 V6）
// ---------- Zero-shot 基线（复用 Day007 V6）----------
string zeroShot = $"""
    你是一位专业的客户体验分析师。请按以下步骤分析评论：
    1. 先识别整体情感倾向（综合考虑正面与负面因素，以核心痛点为准）
    2. 根据情感强度给出0-10评分（评分标准：10=极度满意，7=基本满意，
       5=中性/一般，3=不满意，1=极度不满）
    3. 提取3个最能代表评论主题的关键词（必须是名词或名词短语，不要动词）
    4. 用一句话总结结论（不超过20个中文字，只陈述结论不加建议）

    只输出JSON，不要任何解释。字段：sentiment、score、keywords、conclusion。

    <review>
    {targetReview}
    </review>
    """;

Console.WriteLine("===== Zero-shot 基线（0 个示例）=====");
await RunAndPrint(client, zeroShot);
*/

/*
// 9. 实验二：One-shot（+1 个示例）
// 在 Zero-shot 基础上加 1 个示例。**关键是示例的输出格式要“做规矩”**——你想让模型输出什么样的风格、长度、口径，就在示例里展示什么。
// ---------- One-shot：+1 个示例 ----------
string oneShot = $$"""
    你是一位专业的客户体验分析师。请按以下步骤分析评论：
    1. 先识别整体情感倾向（综合考虑正面与负面因素，以核心痛点为准）
    2. 根据情感强度给出0-10评分（评分标准：10=极度满意，7=基本满意，
       5=中性/一般，3=不满意，1=极度不满）
    3. 提取3个最能代表评论主题的关键词（必须是名词或名词短语，不要动词）
    4. 用一句话总结结论（不超过20个中文字，只陈述结论不加建议）

    只输出JSON，不要任何解释。字段：sentiment、score、keywords、conclusion。

    示例：
    评论：物流超快，包装完好，产品非常好用，强烈推荐！
    输出：{"sentiment":"Positive","score":9,"keywords":["物流快","包装好","产品好用"],"conclusion":"用户对物流、包装和产品都非常满意"}

    <review>
    {{targetReview}}
    </review>
    """;

Console.WriteLine("===== One-shot（1 个示例）=====");
await RunAndPrint(client, oneShot);
*/

/*
// # 10. 实验三：Three-shot（+3 个示例，覆盖三类情感）
// 1 个示例可能不够代表性。加到 3 个，**覆盖 Positive / Neutral / Negative 三种情感**，让模型看到“不同情感下输出长什么样”。
// ---------- Three-shot：+3 个示例（覆盖三类情感）----------
string threeShot = $$"""
    你是一位专业的客户体验分析师。请按以下步骤分析评论：
    1. 先识别整体情感倾向（综合考虑正面与负面因素，以核心痛点为准）
    2. 根据情感强度给出0-10评分（评分标准：10=极度满意，7=基本满意，
       5=中性/一般，3=不满意，1=极度不满）
    3. 提取3个最能代表评论主题的关键词（必须是名词或名词短语，不要动词）
    4. 用一句话总结结论（不超过20个中文字，只陈述结论不加建议）

    只输出JSON，不要任何解释。字段：sentiment、score、keywords、conclusion。

    示例1：
    评论：物流超快，包装完好，产品非常好用，强烈推荐！
    输出：{"sentiment":"Positive","score":9,"keywords":["物流快","包装好","产品好用"],"conclusion":"用户对物流、包装和产品都非常满意"}

    示例2：
    评论：东西收到了，包装完好，目前用着还行，以后再追评。
    输出：{"sentiment":"Neutral","score":5,"keywords":["包装完好","使用体验","待追评"],"conclusion":"用户态度中性，待后续使用验证"}

    示例3：
    评论：质量很差，用了两天就坏了，客服还不理人，非常失望。
    输出：{"sentiment":"Negative","score":1,"keywords":["质量差","易损坏","客服失联"],"conclusion":"产品质量与售后均令用户极度不满"}

    <review>
    {{targetReview}}
    </review>
    """;

Console.WriteLine("===== Three-shot（3 个示例）=====");
await RunAndPrint(client, threeShot);
*/

/*
// # 11. 实验四：Five-shot（+5 个示例，观察边际递减）
// 3 个示例够不够？加到 5 个，观察“质量提升是否边际递减”。
// ---------- Five-shot：+5 个示例 ----------
string fiveShot = $$"""
    你是一位专业的客户体验分析师。请按以下步骤分析评论：
    1. 先识别整体情感倾向（综合考虑正面与负面因素，以核心痛点为准）
    2. 根据情感强度给出0-10评分（评分标准：10=极度满意，7=基本满意，
       5=中性/一般，3=不满意，1=极度不满）
    3. 提取3个最能代表评论主题的关键词（必须是名词或名词短语，不要动词）
    4. 用一句话总结结论（不超过20个中文字，只陈述结论不加建议）

    只输出JSON，不要任何解释。字段：sentiment、score、keywords、conclusion。

    示例1：
    评论：物流超快，包装完好，产品非常好用，强烈推荐！
    输出：{"sentiment":"Positive","score":9,"keywords":["物流快","包装好","产品好用"],"conclusion":"用户对物流、包装和产品都非常满意"}

    示例2：
    评论：东西收到了，包装完好，目前用着还行，以后再追评。
    输出：{"sentiment":"Neutral","score":5,"keywords":["包装完好","使用体验","待追评"],"conclusion":"用户态度中性，待后续使用验证"}

    示例3：
    评论：质量很差，用了两天就坏了，客服还不理人，非常失望。
    输出：{"sentiment":"Negative","score":1,"keywords":["质量差","易损坏","客服失联"],"conclusion":"产品质量与售后均令用户极度不满"}

    示例4：
    评论：价格实惠，功能齐全，就是做工一般，性价比可以。
    输出：{"sentiment":"Neutral","score":6,"keywords":["价格实惠","功能齐全","做工一般"],"conclusion":"性价比尚可，做工有改进空间"}

    示例5：
    评论：用了半年，电池衰减严重，客服推诿不给换，差评。
    输出：{"sentiment":"Negative","score":2,"keywords":["电池衰减","客服推诿","售后差"],"conclusion":"产品耐久性与售后均令用户不满"}

    <review>
    {{targetReview}}
    </review>
    """;

Console.WriteLine("===== Five-shot（5 个示例）=====");
await RunAndPrint(client, fiveShot);
*/

/*
// # 12. 实验五：示例顺序对调（Recency Bias）
// 同样 3 个示例，**正序 vs 反序**，看输出是否不同。这验证 LLM 的“近因偏差”（Recency Bias）——模型更受**最后看到的示例**影响。
// ---------- Five-shot：+5 个示例 ----------
string threeShotReversed = $$"""
    你是一位专业的客户体验分析师。请按以下步骤分析评论：
    1. 先识别整体情感倾向（综合考虑正面与负面因素，以核心痛点为准）
    2. 根据情感强度给出0-10评分（评分标准：10=极度满意，7=基本满意，
       5=中性/一般，3=不满意，1=极度不满）
    3. 提取3个最能代表评论主题的关键词（必须是名词或名词短语，不要动词）
    4. 用一句话总结结论（不超过20个中文字，只陈述结论不加建议）

    只输出JSON，不要任何解释。字段：sentiment、score、keywords、conclusion。

    示例1：
    评论：质量很差，用了两天就坏了，客服还不理人，非常失望。
    输出：{"sentiment":"Negative","score":1,"keywords":["质量差","易损坏","客服失联"],"conclusion":"产品质量与售后均令用户极度不满"}

    示例2：
    评论：东西收到了，包装完好，目前用着还行，以后再追评。
    输出：{"sentiment":"Neutral","score":5,"keywords":["包装完好","使用体验","待追评"],"conclusion":"用户态度中性，待后续使用验证"}

    示例3：
    评论：物流超快，包装完好，产品非常好用，强烈推荐！
    输出：{"sentiment":"Positive","score":9,"keywords":["物流快","包装好","产品好用"],"conclusion":"用户对物流、包装和产品都非常满意"}

    <review>
    {{targetReview}}
    </review>
    """;

Console.WriteLine("===== Three-shot 反序（Negative 在前，Positive 在后）=====");
await RunAndPrint(client, threeShotReversed);
*/

// # 13. 实验六：示例与规则冲突时模型听谁（必做）
// 这是今天最重要的实验。**故意制造规则与示例的冲突**，看模型偏向谁。
// 场景：规则说“评分 10=极度满意”，但示例里有一条 `score=10` 却对应**负面**评论。
// ---------- 示例与规则冲突 ----------
string conflictShot = $$"""
    你是一位专业的客户体验分析师。请按以下步骤分析评论：
    1. 先识别整体情感倾向（综合考虑正面与负面因素，以核心痛点为准）
    2. 根据情感强度给出0-10评分（评分标准：10=极度满意，7=基本满意，
       5=中性/一般，3=不满意，1=极度不满）
    3. 提取3个最能代表评论主题的关键词（必须是名词或名词短语，不要动词）
    4. 用一句话总结结论（不超过20个中文字，只陈述结论不加建议）

    只输出JSON，不要任何解释。字段：sentiment、score、keywords、conclusion。

    示例1：
    评论：质量很差，用了两天就坏了，客服还不理人，非常失望。
    输出：{"sentiment":"Negative","score":10,"keywords":["质量差","易损坏","客服失联"],"conclusion":"产品质量与售后均令用户极度不满"}

    示例2：
    评论：东西收到了，包装完好，目前用着还行，以后再追评。
    输出：{"sentiment":"Neutral","score":5,"keywords":["包装完好","使用体验","待追评"],"conclusion":"用户态度中性，待后续使用验证"}

    示例3：
    评论：物流超快，包装完好，产品非常好用，强烈推荐！
    输出：{"sentiment":"Positive","score":1,"keywords":["物流快","包装好","产品好用"],"conclusion":"用户对物流、包装和产品都非常满意"}

    <review>
    {{targetReview}}
    </review>
    """;

Console.WriteLine("===== 示例与规则冲突（示例里 score=10 是负面）=====");
await RunAndPrint(client, conflictShot);

// ---------- 工具函数 ----------
async Task RunAndPrint(IChatClient c, string prompt)
{
    ChatResponse<ReviewAnalysis> response =
        await c.GetResponseAsync<ReviewAnalysis>(
            prompt,
            new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 4400 });

    if (response.TryGetResult(out ReviewAnalysis? r))
    {
        Console.WriteLine($"  情感：{r.Sentiment}");
        Console.WriteLine($"  评分：{r.Score}/10");
        Console.WriteLine($"  关键词：{string.Join("、", r.Keywords ?? [])}");
        Console.WriteLine($"  结论：{r.Conclusion}");
        Console.WriteLine($"  完整度：{ScoreCompleteness(r)}/4");
    }
    else
    {
        Console.WriteLine($"  解析失败，原始：{response.Text}");
    }
    Console.WriteLine();
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

// ---------- 输出契约 ----------
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