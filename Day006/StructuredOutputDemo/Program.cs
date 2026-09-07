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


string article = """
    RAG（检索增强生成）是一种把外部知识库与大模型结合的技术。
    系统先把文档切分、向量化并存入向量数据库；
    用户提问时，先检索相关片段，再把片段连同问题一起交给大模型生成答案。
    RAG 能显著降低模型幻觉，并让答案可溯源。
    """;

string prompt = $"""
    请阅读下面的文章，并返回一个 JSON 对象，包含：
    - title：文章标题
    - summary：100 字以内中文摘要
    - keywords：3 到 5 个关键词的数组

    文章：
    {article}
    """;
/*
prompt = $"""
    请阅读下面的文章，返回包含：
    - title：文章标题
    - summary：100 字以内中文摘要
    - keywords：3 到 5 个关键词的数组

    文章：
    {article}
    """;
*/    

// 实验一：Prompt 约定 JSON（旧办法，故意踩坑）
/*
ChatResponse response = await client.GetResponseAsync(prompt);

Console.WriteLine("=== 模型原始返回 ===");
Console.WriteLine(response.Text);
Console.WriteLine();

// 程序想直接用，就得自己反序列化
try
{
    ArticleSummary? result =
        JsonSerializer.Deserialize<ArticleSummary>(
            response.Text,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    Console.WriteLine("=== 反序列化成功 ===");
    Console.WriteLine($"标题：{result?.Title}");
    Console.WriteLine($"关键词：{string.Join("、", result?.Keywords ?? [])}");
}
catch (JsonException ex)
{
    Console.WriteLine($"=== 反序列化失败：{ex.Message} ===");
    Console.WriteLine("这就是“Prompt 约定”的脆弱性。");
}
*/

/*
// 实验二：JSON Mode —— ChatResponseFormat.Json
ChatOptions options = new()
{
    ResponseFormat = ChatResponseFormat.Json,
    Temperature = 0.2f
};

ChatResponse response = await client.GetResponseAsync(prompt, options);

Console.WriteLine("=== 模型原始返回 ===");
Console.WriteLine(response.Text);
Console.WriteLine();

ArticleSummary? result =
    JsonSerializer.Deserialize<ArticleSummary>(
        response.Text,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

Console.WriteLine($"标题：{result?.Title}");
Console.WriteLine($"摘要：{result?.Summary}");
Console.WriteLine($"关键词：{string.Join("、", result?.Keywords ?? [])}");
*/

/*
// 实验三：看见 Schema —— AIJsonUtilities.CreateJsonSchema
ChatOptions options = new()
{
    ResponseFormat = ChatResponseFormat.Json,
    Temperature = 0.2f
};

ChatResponse response = await client.GetResponseAsync(prompt, options);

Console.WriteLine("=== 模型原始返回 ===");
Console.WriteLine(response.Text);
Console.WriteLine();

JsonElement schema =
    AIJsonUtilities.CreateJsonSchema(
        typeof(ArticleSummary),
        description: "文章摘要的结构化结果");

Console.WriteLine("=== 自动生成的 JSON Schema ===");
Console.WriteLine(
    JsonSerializer.Serialize(
        schema,
        new JsonSerializerOptions { WriteIndented = true }));
*/

/*
// 实验四：带 Schema 的请求 —— ChatResponseFormatJson
JsonElement schema =
    AIJsonUtilities.CreateJsonSchema(
        typeof(ArticleSummary),
        description: "文章摘要的结构化结果");

ChatOptions options = new()
{
    ResponseFormat = new ChatResponseFormatJson(
        schema,
        schemaName: "ArticleSummary",
        schemaDescription: "文章摘要的结构化结果"),
    Temperature = 0.2f
};

ChatResponse response = await client.GetResponseAsync(prompt, options);

Console.WriteLine("=== 模型原始返回 ===");
Console.WriteLine(response.Text);

ArticleSummary? result =
    JsonSerializer.Deserialize<ArticleSummary>(
        response.Text,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

Console.WriteLine($"标题：{result?.Title}");
Console.WriteLine($"关键词数量：{result?.Keywords?.Count ?? 0}");
*/

/*
// 实验五：今天的主角 —— GetResponseAsync<T>
ChatResponse<ArticleSummary> response =
    await client.GetResponseAsync<ArticleSummary>(
        prompt,
        new ChatOptions { Temperature = 0.2f });

// 推荐写法：先 TryGetResult 做防御
if (response.TryGetResult(out ArticleSummary? result))
{    
    Console.WriteLine($"标题：{result.Title}");
    Console.WriteLine($"摘要：{result.Summary}");
    Console.WriteLine($"关键词：{string.Join("、", result.Keywords ?? [])}");
}
else
{
    Console.WriteLine("模型输出无法解析为 ArticleSummary，原始返回：");
    Console.WriteLine(response.Text);
}
*/


/*
// 实验六：枚举 + 业务场景（官方快速入门同款）
string[] reviews00 =
[
    "这个产品太好用了！物流第二天就到，客服也很耐心，强烈推荐。",
    "质量很差，用了两天就坏了，客服还不理人，非常失望。",
    "东西收到了，包装完好，目前用着还行，以后再追评。"
];

string[] reviews = [
    // ========== 好评（4条，区分满意程度） ==========
    
    // 1. 非常满意（强烈推荐，超出预期）
    "简直是宝藏产品！完全超出了我的预期，做工精致，功能强大，而且客服态度特别好，有问必答。物流也超快，隔天就收到了。这次购物体验近乎完美，已经推荐给身边的朋友了！",

    // 2. 非常满意（惊喜型，会回购）
    "太惊喜了！这个价格能买到这样的品质真的赚到了。使用起来非常顺手，解决了我很久以来的困扰。包装也很用心，没有任何磕碰。肯定会回购，也会持续关注店家新品。",

    // 3. 比较满意（有微小瑕疵但整体好）
    "总体很满意，东西实用，性价比也不错。发货速度快，包装完好。唯一美中不足的是说明书稍微有点简略，研究了一会儿才弄明白，不过不影响使用，还是给好评。",

    // 4. 一般好评（基本合格，没有太多亮点）
    "东西收到了，用了一个星期来评价。目前没发现什么问题，功能都能正常使用，对得起这个价格。没有特别惊艳，但也算中规中矩吧，需要这个功能的朋友可以入手。",

    // ========== 中评（3条，中立或褒贬参半） ==========
    
    // 5. 中评（有优点但缺点明显）
    "产品本身质量还可以，但物流实在太慢了，等了整整一周才到，而且外包装有点变形。好在里面东西没坏。客服回复也不够及时。三星吧，希望改进。",

    // 6. 中评（期望过高，实际一般）
    "可能是我期望太高了，实际收到后感觉没有宣传的那么好。用着还行，但细节处理比较粗糙，边缘有些毛刺。这个价位勉强能接受，但不会推荐给别人。",

    // 7. 中评（还在观察期，未达满意）
    "刚收到时看着不错，但用了两天发现偶尔会卡顿，不知道是不是我操作问题。客服说让我再观察几天。暂时给个中评，如果后续稳定了再来改。",

    // ========== 差评（3条，不同程度的负面） ==========
    
    // 8. 差评（质量严重问题）
    "质量太差了！才用了不到三天就直接坏了，完全没法用。联系客服，半天才回一句，还推卸责任说是我的问题。从来没有这么失望的购物体验，绝对差评，大家慎重购买！",

    // 9. 差评（与描述严重不符）
    "严重虚假宣传！图片上看着很大，实际到手小得可怜，而且材质摸起来很廉价，跟地摊货没什么区别。这价格完全是坑人。申请退货还要我自己出运费，太恶心了。",

    // 10. 差评（服务+质量双重恶劣）
    "一颗星都嫌多。发货慢不说，收到居然是二手货，明显有使用痕迹。找售后直接已读不回，打电话也不接。这店家的服务态度和产品质量都令人发指，千万别踩雷。"
];


foreach (string review in reviews)
{
    string reviewPrompt = $"""
        请分析下面这条产品评论，并以 JSON 格式输出结构化结果。

        评论：{review}
        """;

    ChatResponse<ReviewAnalysis> response =
        await client.GetResponseAsync<ReviewAnalysis>(
            reviewPrompt,
            new ChatOptions { Temperature = 0.1f });

    if (response.TryGetResult(out ReviewAnalysis? r))
    {
        Console.WriteLine(
            $"评论：{review}");
        Console.WriteLine(
            $"  → 情感：{r.Sentiment}，评分：{r.Score}/10，结论：{r.Conclusion}");
        Console.WriteLine();
    }
    else
    {
        Console.WriteLine($"解析失败：{response.Text}");
    }
}
*/



/*
// 实验七：结构化输出 + Streaming（衔接 Day005）
ChatOptions options = new()
{
    ResponseFormat = ChatResponseFormat.Json,
    Temperature = 0.2f
};

// string article = File.ReadAllText("article.md");

string streamPrompt = $"""
    请阅读下面的文章，以 JSON 格式输出结构化结果，
    包含 title（标题）、summary（摘要）、keywords（关键词数组）。

    文章：{article}
    """;

string fullText = "";

Console.WriteLine("=== 流式接收 JSON ===");
await foreach (ChatResponseUpdate update in
    client.GetStreamingResponseAsync(streamPrompt, options))
{
    Console.Write(update.Text);   // 边生成边显示
    fullText += update.Text;
}
Console.WriteLine();

// 流结束后，整体反序列化
ArticleSummary? streamed =
    JsonSerializer.Deserialize<ArticleSummary>(
        fullText,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

Console.WriteLine();
Console.WriteLine($"=== 结构化结果 ===");
Console.WriteLine($"标题：{streamed?.Title}");
Console.WriteLine($"关键词：{string.Join("、", streamed?.Keywords ?? [])}");
*/

// 实验八：失败与防御（必做）
ChatResponse<ArticleSummary> response =
    await client.GetResponseAsync<ArticleSummary>(
        prompt,
        new ChatOptions
        {
            Temperature = 0.2f,
            MaxOutputTokens = 60   // 故意给很小
        });

// 15.1 MaxOutputTokens 太小 → JSON 被截断
Console.WriteLine($"原始返回：{response.Text}");
Console.WriteLine($"TryGetResult：{response.TryGetResult(out _)}");

// 15.2 模型不遵守 Schema 时怎么办
if (response.TryGetResult(out ArticleSummary? result))
{
    // 正常路径：用强类型对象
    // SaveToDatabase(result);
    Console.WriteLine($"=========模型符合 Schema ==========");
    Console.WriteLine($"标题：{result.Title}");
    Console.WriteLine($"摘要：{result.Summary}");
    Console.WriteLine($"关键词：{string.Join("、", result.Keywords ?? [])}");
    Console.WriteLine();
}
else
{
    // 失败路径：记录原始文本，便于排查 / 重试 / 降级
    // _logger.LogWarning("结构化输出解析失败：{Text}", response.Text);
    Console.WriteLine($"=========模型不遵守 Schema ==========");
    Console.WriteLine($"解析失败：{response.Text}");
    // 生产中可以：重试一次 / 换模型 / 退回纯文本展示
}


// 定义我们想要的输出结构。
// 注意：使用 top-level statements 时，类型声明必须放在所有语句之后。
public class ArticleSummary
{
    [Description("文章标题")]
    public string? Title { get; set; }

    [Description("100 字以内的中文摘要")]
    public string? Summary { get; set; }

    [Description("3 到 5 个关键词")]
    public List<string>? Keywords { get; set; }
}


public enum Sentiment
{
    Positive,   // 正面
    Negative,   // 负面
    Neutral     // 中性
}

public class ReviewAnalysis
{
    [Description("情感倾向，只能是 Positive、Negative、Neutral 之一")]
    public Sentiment Sentiment { get; set; }

    [Description("0 到 10 的情感强度评分，整数, Sentiment 为 Positive 时，Score 越高，Negative 时，Score 越低")]
    public int Score { get; set; }

    [Description("一句话中文结论")]
    public string? Conclusion { get; set; }
}
