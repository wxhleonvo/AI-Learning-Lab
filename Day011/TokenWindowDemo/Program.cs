/*
using System.Text;
using Tiktoken;

System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
Console.InputEncoding = System.Text.Encoding.GetEncoding(936);


// cl100k_base 是 GPT-4 / text-embedding-3 等模型用的分词器
// qwen 系列用的是自己的分词器，但 cl100k_base 作为近似估算足够
var encoder = Tiktoken.Encoding.Get("cl100k_base");

Console.WriteLine("===== 实验一：Token vs 字符数 =====");

string[] samples =
{
    "Hello, World!",
    "你好，世界！",
    "The quick brown fox jumps over the lazy dog.",
    "敏捷的棕色狐狸跳过了懒狗。",
    "LLM Token Context Window",
    "大语言模型 Token 上下文窗口",
};

foreach (var s in samples)
{
    var tokens = encoder.Encode(s);
    Console.WriteLine($"  字符数={s.Length,3}  Token数={tokens.Count,3}  比值={tokens.Count / (double)s.Length:F2}  文本：{s}");
}

### 预期结果（近似）

```text
字符数=13  Token数= 4  比值=0.31  文本：Hello, World!
字符数= 6  Token数= 6  比值=1.00  文本：你好，世界！
字符数=44  Token数= 9  比值=0.20  文本：The quick brown fox...
字符数=12  Token数=12  比值=1.00  文本：敏捷的棕色狐狸跳过了懒狗。
```

### 必须得出的结论

```text
英文：1 词 ≈ 1 Token（4 字符 ≈ 1 Token）
中文：1 字 ≈ 1–2 Token
中英文混排：中文 Token 密度远高于英文
所以：「输入 1000 中文字 ≈ 1000–2000 Token」，不是 1000

*/

using System.ComponentModel;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;
using Tiktoken;

System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
Console.InputEncoding = System.Text.Encoding.GetEncoding(936);

// cl100k_base 是 GPT-4 / text-embedding-3 等模型用的分词器
// qwen 系列用的是自己的分词器，但 cl100k_base 作为近似估算足够
var encoder = Tiktoken.Encoding.Get("cl100k_base");

IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>()
    .Build();

string? model = config["DashScopeModel"];
string? key = config["DashScopeKey"];

OpenAIClientOptions clientOptions = new()
{
    Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1")
};

IChatClient client = new OpenAIClient(new ApiKeyCredential(key!), clientOptions)
    .GetChatClient(model).AsIChatClient();


/*
Console.WriteLine("\n===== 实验二：ChatResponse.Usage 真实 Token 消耗 =====");

string prompt = "请用一句话解释什么是 Token。";
var response = await client.GetResponseAsync(prompt, new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 500 });


Console.WriteLine($"  模型：{model}");
Console.WriteLine($"  输入 Token（PromptTokens）：{response.Usage?.InputTokenCount}");
Console.WriteLine($"  输出 Token（OutputTokens）：{response.Usage?.OutputTokenCount}");
Console.WriteLine($"  总 Token（TotalTokens）：{response.Usage?.TotalTokenCount}");
Console.WriteLine($"  推理 Token（ReasoningTokens）：{response.Usage?.ReasoningTokenCount}");

// 判断响应状态，分情况输出
string reply = response.Text ?? string.Empty;
string finishReason = response.FinishReason?.ToString() ?? "Unknown";
long reasoningTokens = response.Usage?.ReasoningTokenCount ?? 0;
long outputTokens = response.Usage?.OutputTokenCount ?? 0;
int maxOutput = 200;

Console.WriteLine($"  FinishReason：{finishReason}");
if (string.IsNullOrEmpty(reply))
{
    Console.WriteLine($"  [警告] 回复为空！FinishReason={finishReason}");
    if (finishReason.Equals("Length", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"  诊断：输出被 MaxOutputTokens={maxOutput} 截断");
        Console.WriteLine($"        推理 Token {reasoningTokens} / 输出 Token {outputTokens} —— 推理链挤占了输出预算");
        Console.WriteLine("  建议：1) 增大 MaxOutputTokens；2) 通过 AdditionalProperties 传 thinking=disabled 关闭推理");
    }
    else if (finishReason.Equals("ContentFilter", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("  诊断：内容被安全策略过滤");
    }
    else
    {
        Console.WriteLine($"  诊断：非正常结束，请检查模型/网络或参数配置");
    }
}
else
{
    Console.WriteLine($"  回复：{reply}");
}

// 用 Tiktoken 本地估算输入 Token（与 API 返回对比）
int localTokenCount = encoder.Encode(prompt).Count;
Console.WriteLine($"\n  Tiktoken 本地估算输入 Token：{localTokenCount}");
Console.WriteLine($"  API 报告输入 Token：{response.Usage?.InputTokenCount}");
Console.WriteLine($"  差异说明：API 会加上 role 标记、特殊 token，所以比本地估算多几个");
*/

/*
// # 7. 实验三：Context Window 边界探测（故意超限）

//qwen3.7-flash 的 Context Window 是 **128K**。构造一个 200K 字符的 Prompt，观察 400 错误。

Console.WriteLine("\n===== 实验三：Context Window 边界探测 =====");

// 构造超长 Prompt：重复一段文本直到远超 128K Token
// 注意：字符数 ≠ Token 数。中文 1 字符 ≈ 0.5 Token（cl100k_base），
//   5000 次只产生 ~8 万 Token，根本没到 128K 上限，所以不报 context_length_exceeded。
//   要触发上限，需要 ≥ 10000 次重复。
int repeatCount = 1;
string longText = string.Join("\n", Enumerable.Repeat("这是一段测试文本，用于测试 Context Window 上限。", repeatCount));
int estimatedTokens = encoder.Encode(longText).Count;
Console.WriteLine($"  构造的 Prompt 字符数：{longText.Length}");
Console.WriteLine($"  Tiktoken 估算 Token 数：{estimatedTokens}（128K 上限 = 131072，是否超出：{estimatedTokens > 131072}）");

try
{
    var r = await client.GetResponseAsync(longText, new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 100 });

    // 关键：没抛异常 ≠ 真正成功。要检查 r.Text 是否为空 + FinishReason。
    string reply = r.Text ?? string.Empty;
    string finishReason = r.FinishReason?.ToString() ?? "Unknown";
    Console.WriteLine($"  FinishReason：{finishReason}");
    if (string.IsNullOrEmpty(reply))
    {
        Console.WriteLine($"  [警告] 调用未抛异常，但回复为空！FinishReason={finishReason}");
        Console.WriteLine("        请求通过了 Context Window 检查，但输出被截断（参考实验二）");
    }
    else
    {
        Console.WriteLine($"  意外成功（有内容）：{reply[..Math.Min(50, reply.Length)]}...");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"  异常类型：{ex.GetType().Name}");
    Console.WriteLine($"  消息：{ex.Message[..Math.Min(200, ex.Message.Length)]}");
    // 典型：context_length_exceeded / maximum context length
}
*/

/*
// # 8. 实验四：推理模型的 Reasoning Tokens 挤占预算
// 切到 glm-5.2，观察 `Usage.ReasoningTokenCount` 如何挤占 `MaxOutputTokens`。这是 Day009 踩的坑的量化版本。
Console.WriteLine("\n===== 实验四：推理模型 Reasoning Tokens 挤占预算 =====");

// 临时切到 glm-5.2
IChatClient glmClient = new OpenAIClient(new ApiKeyCredential(key!), clientOptions)
    .GetChatClient("glm-5.3").AsIChatClient();

string analyzePrompt = """
    请分析下面这条评论的情感，输出 JSON：{"sentiment":"...","score":0}
    评论：这个产品续航不行，电池衰减太快了，失望。
    """;

// 故意给小预算，观察 reasoning tokens 挤占
int[] budgets = { 200, 500, 1000, 2000, 4000 };
foreach (var budget in budgets)
{
    try
    {
        var r = await glmClient.GetResponseAsync(analyzePrompt,
            new ChatOptions { Temperature = 0.1f, MaxOutputTokens = budget });
        Console.WriteLine($"  预算={budget,5}  FinishReason={r.FinishReason,-8} " +
                          $"OutputTokens={r.Usage?.OutputTokenCount,5} " +
                          $"ReasoningTokens={r.Usage?.ReasoningTokenCount,5} " +
                          $"TextLen={r.Text?.Length ?? 0,4}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  预算={budget,5}  异常：{ex.Message[..Math.Min(80, ex.Message.Length)]}");
    }
}
*/

/*
// # 9. 实验五：Prompt Token 预算策略（模板化场景）
//把 Day009 的模板链路拿过来，拆解各部分的 Token 占比，制定预算。
Console.WriteLine("\n===== 实验五：Prompt Token 预算拆解 =====");

string systemPrompt = "你是一位专业的客户体验分析师。请按步骤分析评论并输出 JSON。";
string fewShotExample = """
    评论：物流超快，包装完好，产品非常好用，强烈推荐！
    输出：{"sentiment":"Positive","score":9,"keywords":["物流快","包装好","产品好用"]}
    """;
string userInput = "收到货第三天了，物流确实快，包装也很结实没破损。但是续航比宣传的少很多...";

int systemTokens = encoder.Encode(systemPrompt).Count;
int exampleTokens = encoder.Encode(fewShotExample).Count;
int inputTokens = encoder.Encode(userInput).Count;

Console.WriteLine($"  系统提示 Token：{systemTokens}");
Console.WriteLine($"  1 个 Few-shot 示例 Token：{exampleTokens}");
Console.WriteLine($"  用户输入 Token：{inputTokens}");
Console.WriteLine($"  加 3 个示例后总输入 Token：{systemTokens + exampleTokens * 3 + inputTokens}");
Console.WriteLine($"  加 10 个示例后总输入 Token：{systemTokens + exampleTokens * 10 + inputTokens}");
Console.WriteLine($"\n  qwen3.7-flash Context Window = 128K");
Console.WriteLine($"  剩余给输出的预算 = 128000 - 输入 Token");
*/

// # 10. 实验六：对话历史 Token 累积与截断策略
// 多轮对话（Day003）中，历史消息会累积 Token。演示「滑动窗口」截断策略。
Console.WriteLine("\n===== 实验六：对话历史 Token 累积与截断 =====");

// 模拟 10 轮对话历史
List<ChatMessage> history = new();
for (int i = 0; i < 10; i++)
{
    history.Add(new ChatMessage(ChatRole.User, $"第{i + 1}轮问题：这是一段比较长的用户输入内容，用于测试历史 Token 累积。".PadRight(100, 'x')));
    history.Add(new ChatMessage(ChatRole.Assistant, $"第{i + 1}轮回复：这是一段比较长的助手回复内容，用于测试历史 Token 累积。".PadRight(100, 'x')));
}

// 计算完整历史的 Token
string fullHistoryText = string.Join("\n", history.Select(m => $"{m.Role}: {m.Text}"));
int fullTokens = encoder.Encode(fullHistoryText).Count;
Console.WriteLine($"  10 轮完整历史 Token：{fullTokens}");

// 滑动窗口：只保留最近 4 轮（8 条消息）
int keepRounds = 4;
var trimmed = history.Skip((10 - keepRounds) * 2).ToList();
string trimmedText = string.Join("\n", trimmed.Select(m => $"{m.Role}: {m.Text}"));
int trimmedTokens = encoder.Encode(trimmedText).Count;
Console.WriteLine($"  保留最近 {keepRounds} 轮 Token：{trimmedTokens}（节省 {fullTokens - trimmedTokens}）");

// 更激进：保留最近 2 轮
var moreTrimmed = history.Skip((10 - 2) * 2).ToList();
int moreTrimmedTokens = encoder.Encode(string.Join("\n", moreTrimmed.Select(m => m.Text))).Count;
Console.WriteLine($"  保留最近 2 轮 Token：{moreTrimmedTokens}（节省 {fullTokens - moreTrimmedTokens}）");

Console.WriteLine("\n  截断策略优先级：");
Console.WriteLine("    ① 滑动窗口（保留最近 N 轮）——最简单，丢早期上下文");
Console.WriteLine("    ② 摘要压缩（用 LLM 把早期历史总结成一段）——保信息但耗 Token");
Console.WriteLine("    ③ 混合（早期摘要 + 近期原文）——生产常用");
