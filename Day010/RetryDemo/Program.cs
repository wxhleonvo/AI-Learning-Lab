using System.ComponentModel;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using OpenAI;
using System.ClientModel;

// ## 创建 ResiliencePipeline
// using System.Net;
using Polly;
using Polly.Retry;  // RetryStrategyOptions 在此命名空间（Polly 8 拆分：核心在 Polly，策略选项在子命名空间）


Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.InputEncoding = Encoding.GetEncoding(936);

IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>()
    .Build();

string? model = config["DashScopeModel"];
string? realKey = config["DashScopeKey"];

OpenAIClientOptions clientOptions = new()
{
    Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1")
};

IChatClient client = new OpenAIClient(new ApiKeyCredential(realKey!), clientOptions)
    .GetChatClient(model).AsIChatClient();


/*
// ---------- 实验一：错误类型识别 ----------
Console.WriteLine("===== 实验一：错误类型识别 =====");
// 1. 错误 Key → 401 Unauthorized
Console.WriteLine("\n1. 错误 Key → 预期 401：");
IChatClient badClient = new OpenAIClient(new ApiKeyCredential("sk-invalid-key"), clientOptions)
    .GetChatClient(model).AsIChatClient();
try
{
    await badClient.GetResponseAsync("你好", new ChatOptions { Temperature = 0.1f });
    Console.WriteLine("  意外：没报错");
}
catch (Exception ex)
{
    ClassifyException(ex);
}
// 2. 超长 Prompt → 400 Bad Request
Console.WriteLine("\n2. 超长 Prompt → 预期 400 或 context length exceeded：");
string hugePrompt = new string('x', 100_000_000);
Console.WriteLine($"  Prompt 长度：{hugePrompt.Length}");
try
{
    await client.GetResponseAsync(hugePrompt, new ChatOptions { Temperature = 0.1f });
    Console.WriteLine("  意外：没报错");
}
catch (Exception ex)
{
    ClassifyException(ex);
}
*/

/*
// # 6. 实验二：指数退避 + 抖动（手写实现）
// ---------- 实验二：手写重试 ----------
Console.WriteLine("\n===== 实验二：手写指数退避 + 抖动 =====");
// 正常调用（验证路径走通）
ChatResponse<ReviewAnalysis> result = await InvokeWithRetryAsync(
    client,
    "你好，请用 JSON 返回 {\"sentiment\":\"Positive\",\"score\":9}",
    new ChatOptions { Temperature = 0.1f });

if (result.TryGetResult(out ReviewAnalysis? r))
    Console.WriteLine($"  成功：情感={r.Sentiment}, 评分={r.Score}");


// ---------- 手写重试实现 ----------
static async Task<ChatResponse<ReviewAnalysis>> InvokeWithRetryAsync(
    IChatClient chatClient,
    string prompt,
    ChatOptions options,
    int maxRetries = 3,
    CancellationToken ct = default)
{
    int attempt = 0;
    Exception? lastException = null;

    while (attempt <= maxRetries)
    {
        try
        {
            attempt++;
            Console.WriteLine($"  [重试 {attempt}/{maxRetries + 1}] 调用中...");

            var response = await chatClient.GetResponseAsync<ReviewAnalysis>(
                prompt, options, cancellationToken: ct);

            // 结构化输出解析失败：降级处理
            if (!response.TryGetResult(out _))
            {
                // 不是网络错误，是模型输出问题 → 不重试，直接抛业务异常
                throw new InvalidOperationException(
                    $"结构化输出解析失败，FinishReason={response.FinishReason}，" +
                    $"原始：{response.Text?.Substring(0, Math.Min(100, response.Text?.Length ?? 0))}");
            }

            Console.WriteLine($"  [尝试 {attempt}] ✅ 成功");
            return response;
        }
        catch (Exception ex) when (IsRetryable(ex))
        {
            lastException = ex;
            if (attempt > maxRetries) break;

            // 指数退避 + 抖动
            // base=1s, attempt=1→1s, 2→2s, 3→4s, 4→8s
            double delayMs = 1000 * Math.Pow(2, attempt - 1);
            // + random jitter [0, delay] 避免惊群
            double jitterMs = new Random().NextDouble() * delayMs;
            double totalMs = delayMs + jitterMs;

            Console.WriteLine($"  [尝试 {attempt}] ⏳ 可重试错误：{ex.GetType().Name}，" +
                              $"等待 {totalMs/1000:F1}s 后重试");
            await Task.Delay((int)totalMs, ct);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine($"  [尝试 {attempt}] ❌ 超时，不再重试");
            throw;  // 业务超时直接抛，不重试
        }
        catch
        {
            Console.WriteLine($"  [尝试 {attempt}] ❌ 不可重试错误");
            throw;  // 不可重试的错误直接抛
        }
    }

    throw new InvalidOperationException(
        $"重试 {maxRetries + 1} 次后仍失败", lastException);
}
*/

/*
// # 7. 实验三：用错误 Key 触发重试循环（验证可重试逻辑）
// 用正确 Key 跑不出重试，现在制造 429 或 500 来验证退避行为。最简单的办法：**让同一个错误 Key 快速触发限流**，或者**模拟 Polly 风格的断路器禁用**。
// 但今天不用真的触发限流（会烧额度），而是**把 `IsRetryable` 临时放宽**，让 401 也进入重试循环，观察退避延迟：
// 临时：让 401 也可重试（仅用于演示退避行为）
IChatClient badClient = new OpenAIClient(new ApiKeyCredential("sk-invalid-key"), clientOptions)
    .GetChatClient(model).AsIChatClient();

static bool IsRetryableDemo(Exception ex) => ex switch
{

    HttpRequestException => true,     // 演示：所有 HTTP 错误都重试
    ClientResultException => true, 
    _                    => false
};

Console.WriteLine("\n===== 实验三：退避行为演示（故意让 401 也重试）====");
Console.WriteLine("（临时放宽 IsRetryable，让错误 Key 触发退避循环）");

try
{
    await InvokeWithRetryDemoAsync(badClient, "你好", new ChatOptions { Temperature = 0.1f });
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"  最终：{ex.Message}");
}
static async Task<ChatResponse<ReviewAnalysis>> InvokeWithRetryDemoAsync(
    IChatClient chatClient,
    string prompt,
    ChatOptions options,
    int maxRetries = 3,
    CancellationToken ct = default)
{
    int attempt = 0;
    Exception? lastException = null;

    while (attempt <= maxRetries)
    {
        try
        {
            attempt++;
            Console.WriteLine($"  [重试 {attempt}/{maxRetries + 1}] 调用中...");

            var response = await chatClient.GetResponseAsync<ReviewAnalysis>(
                prompt, options, cancellationToken: ct);

            // 结构化输出解析失败：降级处理
            if (!response.TryGetResult(out _))
            {
                // 不是网络错误，是模型输出问题 → 不重试，直接抛业务异常
                throw new InvalidOperationException(
                    $"结构化输出解析失败，FinishReason={response.FinishReason}，" +
                    $"原始：{response.Text?.Substring(0, Math.Min(100, response.Text?.Length ?? 0))}");
            }

            Console.WriteLine($"  [尝试 {attempt}] ✅ 成功");
            return response;
        }
        catch (Exception ex) when (IsRetryableDemo(ex))
        {
            lastException = ex;
            if (attempt > maxRetries) break;

            // 指数退避 + 抖动
            // base=1s, attempt=1→1s, 2→2s, 3→4s, 4→8s
            double delayMs = 1000 * Math.Pow(2, attempt - 1);
            // + random jitter [0, delay] 避免惊群
            double jitterMs = new Random().NextDouble() * delayMs;
            double totalMs = delayMs + jitterMs;

            Console.WriteLine($"  [尝试 {attempt}] ⏳ 可重试错误：{ex.GetType().Name}，" +
                              $"等待 {totalMs/1000:F1}s 后重试");
            await Task.Delay((int)totalMs, ct);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine($"  [尝试 {attempt}] ❌ 超时，不再重试");
            throw;  // 业务超时直接抛，不重试
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ex={ex} ,IsRetryableDemo(ex)={IsRetryableDemo(ex)}");
            Console.WriteLine($"  [尝试 {attempt}] ❌ 不可重试错误");
            throw;  // 不可重试的错误直接抛
        }
    }

    throw new InvalidOperationException(
        $"重试 {maxRetries + 1} 次后仍失败", lastException);
}
*/

/*
// # 8. 实验四：整体超时 + 取消（CancellationTokenSource）
// 重试循环必须受**整体超时**约束。比如设 30 秒硬限制，无论重试到第几次，30 秒到了必须停。
// ---------- 实验四：整体超时 ----------
Console.WriteLine("\n===== 实验四：整体超时控制 =====");

IChatClient badClient = new OpenAIClient(new ApiKeyCredential("sk-invalid-key"), clientOptions)
    .GetChatClient(model).AsIChatClient();

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));  // 5 秒硬限制

try
{
    // 用 badClient + 放宽 IsRetryable，触发多次退避但 5 秒后被取消
    await InvokeWithRetryAsync(badClient, "你好",
        new ChatOptions { Temperature = 0.1f },
        maxRetries: 10,  // 故意多设，实际会被 5s 超时截断
        ct: cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("  ⏱️ 5 秒整体超时，正确取消");
}
// ---------- 手写重试实现 ----------
static async Task<ChatResponse<ReviewAnalysis>> InvokeWithRetryAsync(
    IChatClient chatClient,
    string prompt,
    ChatOptions options,
    int maxRetries = 3,
    CancellationToken ct = default)
{
    int attempt = 0;
    Exception? lastException = null;

    while (attempt <= maxRetries)
    {
        try
        {
            attempt++;
            Console.WriteLine($"  [重试 {attempt}/{maxRetries + 1}] 调用中...");

            var response = await chatClient.GetResponseAsync<ReviewAnalysis>(
                prompt, options, cancellationToken: ct);

            // 结构化输出解析失败：降级处理
            if (!response.TryGetResult(out _))
            {
                // 不是网络错误，是模型输出问题 → 不重试，直接抛业务异常
                throw new InvalidOperationException(
                    $"结构化输出解析失败，FinishReason={response.FinishReason}，" +
                    $"原始：{response.Text?.Substring(0, Math.Min(100, response.Text?.Length ?? 0))}");
            }

            Console.WriteLine($"  [尝试 {attempt}] ✅ 成功");
            return response;
        }
        catch (Exception ex) when (IsRetryable(ex))
        {
            lastException = ex;
            if (attempt > maxRetries) break;

            // 指数退避 + 抖动
            // base=1s, attempt=1→1s, 2→2s, 3→4s, 4→8s
            double delayMs = 1000 * Math.Pow(2, attempt - 1);
            // + random jitter [0, delay] 避免惊群
            double jitterMs = new Random().NextDouble() * delayMs;
            double totalMs = delayMs + jitterMs;

            Console.WriteLine($"  [尝试 {attempt}] ⏳ 可重试错误：{ex.GetType().Name}，" +
                              $"等待 {totalMs / 1000:F1}s 后重试");
            await Task.Delay((int)totalMs, ct);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine($"  [尝试 {attempt}] ❌ 超时，不再重试");
            throw;  // 业务超时直接抛，不重试
        }
        catch
        {
            Console.WriteLine($"  [尝试 {attempt}] ❌ 不可重试错误");
            throw;  // 不可重试的错误直接抛
        }
    }

    throw new InvalidOperationException(
        $"重试 {maxRetries + 1} 次后仍失败", lastException);
}
*/


/*
// # 9. 实验五：结构化输出降级（FinishReason 分类处理）
// Day009 你见过：`FinishReason=length` + RawText 为空。这不是网络错误，重试没用。今天把它纳入异常链路，**降级为 RawText 回退**（结构化解析失败时返回原始文本，允许上层决定要不要展示给用户）。
// ---------- 实验五：FinishReason 分类处理 ----------
static async Task<(ReviewAnalysis? Structured, string? RawText, ChatFinishReason? Reason)>
    InvokeWithGracefulDegradationAsync(
    IChatClient chatClient,
    string prompt,
    ChatOptions options,
    CancellationToken ct = default)
{
    try
    {
        var response = await chatClient.GetResponseAsync<ReviewAnalysis>(prompt, options, cancellationToken: ct);

        if (response.TryGetResult(out ReviewAnalysis? r))
        {
            return (r, response.Text, response.FinishReason);
        }

        // 结构化解析失败，但有原始文本 → 降级
        if (!string.IsNullOrWhiteSpace(response.Text))
        {
            Console.WriteLine($"  [降级] 结构化解析失败，FinishReason={response.FinishReason}，" +
                              $"返回原始文本（{response.Text.Length} 字符）");
            return (null, response.Text, response.FinishReason);
        }

        // RawText 也为空 → 严重问题
        return (null, null, response.FinishReason);
    }
    catch (Exception ex) when (!IsRetryable(ex))
    {
        Console.WriteLine($"  [异常] {ex.GetType().Name}: {ex.Message[..Math.Min(100, ex.Message.Length)]}");
        return (null, null, null);
    }
}


// ### 调用示例    
string prompt = "请分析这条评论的情感，只输出 JSON：这个产品续航不行，电池衰减太快了，失望。";

var (structured, raw, reason) = await InvokeWithGracefulDegradationAsync(
    client, prompt, new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 400 });

// 测试其他错误场景，如：max_output_tokens 过小
//var (structured, raw, reason) = await InvokeWithGracefulDegradationAsync(
//    client, prompt, new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 40 });

// 测试其他错误，如 API 错误、超时等;
//IChatClient badClient = new OpenAIClient(new ApiKeyCredential("sk-invalid-key"), clientOptions)
//    .GetChatClient(model).AsIChatClient();
//var (structured, raw, reason) = await InvokeWithGracefulDegradationAsync(
//    badClient, prompt, new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 400 });


Console.WriteLine($"FinishReason={reason} {reason}");
if (structured is not null)
    Console.WriteLine($"✅ 结构化成功：{structured.Sentiment} / {structured.Score}");
else if (raw is not null)
    Console.WriteLine($"⚠️ 降级到 RawText（{reason}）：{raw[..Math.Min(80, raw.Length)]}…");
else
    Console.WriteLine($"❌ 完全失败（{reason}）");
*/


/*
// # 10. 实验六：Microsoft.Extensions.Http.Resilience（Polly 8）
// 手写重试够用，但生产项目用官方 Resilience 库（Polly 8 封装）。优势：声明式配置、可组合、已内置指数退避 + 抖动。

// ---------- 实验六：Resilience Pipeline ----------
Console.WriteLine("\n===== 实验六：Polly 8 Resilience Pipeline =====");
// Polly 8 不直接包 IChatClient，而是包整个代码块
var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        ShouldHandle = new PredicateBuilder()  // 非泛型版，配合非泛型 RetryStrategyOptions（PredicateBuilder<Exception> 会与基于 object 的 ShouldHandle 类型不匹配）
            .Handle<HttpRequestException>(e => e.StatusCode is HttpStatusCode.TooManyRequests
                                                    or HttpStatusCode.InternalServerError
                                                    or HttpStatusCode.BadGateway
                                                    or HttpStatusCode.ServiceUnavailable)
            .Handle<ClientResultException>(e=>e.Status is 429 or 401 or 400 or 403 or >= 500 and <= 599)
            .Handle<TaskCanceledException>(),
        DelayGenerator = args =>
        {
            double delayMs = 1000 * Math.Pow(2, args.AttemptNumber);
            double jitterMs = new Random().NextDouble() * delayMs;
            return new ValueTask<TimeSpan?>(TimeSpan.FromMilliseconds(delayMs + jitterMs));
        },
        OnRetry = args =>
        {
            Console.WriteLine($"  [Polly] 第 {args.AttemptNumber + 1} 次重试，" +
                              $"等待 {args.RetryDelay.TotalSeconds:F1}s，原因：{args.Outcome.Exception?.GetType().Name}");
            return default;
        }
    })
    .Build();

// ## 在 RetryStrategyOptions 里调用
// Polly 8 不直接包 IChatClient，而是包整个代码块

var pollyResult = await pipeline.ExecuteAsync(async ct =>
{
    Console.WriteLine("  [Polly] 执行调用...");
    return await client.GetResponseAsync<ReviewAnalysis>(
        "你好，请用 JSON 返回 sentiment/score/keywords/conclusion",
        new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 1000 },
        cancellationToken: ct);
}, CancellationToken.None);

// 测试其他错误场景，如 API 错误、超时等;
//IChatClient badClient = new OpenAIClient(new ApiKeyCredential("sk-invalid-key"), clientOptions)
//    .GetChatClient(model).AsIChatClient();
//var pollyResult = await pipeline.ExecuteAsync(async ct =>
//{
//    Console.WriteLine("  [Polly] 执行调用...");
//    return await badClient.GetResponseAsync<ReviewAnalysis>(
//        "你好，请用 JSON 返回 sentiment/score/keywords/conclusion",
//        new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 1000 },
//        cancellationToken: ct);
//}, CancellationToken.None);


if (pollyResult.TryGetResult(out ReviewAnalysis? pr))
    Console.WriteLine($"  [Polly] ✅ 成功：情感={pr.Sentiment}");
else
    Console.WriteLine($"  [Polly] ❌ 失败：{pollyResult.Messages}");
*/


//# 11. 实验七：Day009 模板链路 + 重试保护层（整合）
//把 Day009 的模板渲染链路和今天的重试保护层整合，形成完整的「模板 → 渲染 → 带重试调用 → 优雅降级」生产级链路。
//复用 Day009 的资源（Prompts.txt、examples.json），直接 `File.Copy` 过来：
// ---------- Day009 模板链路（简化版）----------

static async Task<(ReviewAnalysis? Structured, string? RawText, ChatFinishReason? Reason)>
    InvokeWithGracefulDegradationAsync(
    IChatClient chatClient,
    string prompt,
    ChatOptions options,
    CancellationToken ct = default)
{
    try
    {
        var response = await chatClient.GetResponseAsync<ReviewAnalysis>(prompt, options, cancellationToken: ct);

        if (response.TryGetResult(out ReviewAnalysis? r))
        {
            return (r, response.Text, response.FinishReason);
        }

        // 结构化解析失败，但有原始文本 → 降级
        if (!string.IsNullOrWhiteSpace(response.Text))
        {
            Console.WriteLine($"  [降级] 结构化解析失败，FinishReason={response.FinishReason}，" +
                              $"返回原始文本（{response.Text.Length} 字符）");
            return (null, response.Text, response.FinishReason);
        }

        // RawText 也为空 → 严重问题
        return (null, null, response.FinishReason);
    }
    catch (Exception ex) when (!IsRetryable(ex))
    {
        Console.WriteLine($"  [异常] {ex.GetType().Name}: {ex.Message[..Math.Min(100, ex.Message.Length)]}");
        return (null, null, null);
    }
}

static string RenderSimple(string template, Dictionary<string, string?> variables)
{
    ValidateVariables(template, variables);
    return Regex.Replace(template, @"\{\{\$?(\w+)\}\}", match =>
    {
        string name = match.Groups[1].Value;
        return variables.TryGetValue(name, out string? v) && v is not null
            ? v
            : throw new InvalidOperationException($"缺少变量 {name}");
    });
}
static void ValidateVariables(string template, Dictionary<string, string?> variables)
{
    var required = Regex.Matches(template, @"\{\{\$?(\w+)\}\}")
        .Select(m => m.Groups[1].Value).ToHashSet();
    var missing = required.Where(n => !variables.ContainsKey(n)).ToList();
    if (missing.Count > 0)
        throw new InvalidOperationException($"模板缺少变量 {string.Join(", ", missing)}");
}

// ### 整合后的主循环
// ---------- 整合：模板渲染 + 重试 + 降级 ----------
string template = await File.ReadAllTextAsync("Prompts/review-analysis.v1.txt");

var jsonOpts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
jsonOpts.Converters.Add(new JsonStringEnumConverter());
List<FewShotExample> examples = JsonSerializer.Deserialize<List<FewShotExample>>(
    await File.ReadAllTextAsync("examples.json"), jsonOpts)!;

string role = config["Prompt:Role"] ?? "专业的客户体验分析师";
string input = "这个产品续航不行，电池衰减太快了，失望。";

string BuildExamplesSection(List<FewShotExample> exs)
{
    if (exs.Count == 0) return "";
    var sb = new StringBuilder();
    foreach (var ex in exs)
    {
        sb.AppendLine($"评论：{ex.Review}");
        sb.AppendLine($"输出：{JsonSerializer.Serialize(ex.Output, jsonOpts)}");
    }
    return sb.ToString();
}

var vars = new Dictionary<string, string?>
{
    ["role"] = role,
    ["examples"] = BuildExamplesSection(examples.Take(3).ToList()),
    ["input"] = input,
};

string prompt = RenderSimple(template, vars);

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

// 关键：两层保护
var (structured, raw, reason) = await InvokeWithGracefulDegradationAsync(
    client, prompt,
    new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 4000 },
    cts.Token);

if (structured is not null)
{
    Console.WriteLine($"✅ 成功：情感={structured.Sentiment} / 评分={structured.Score} " +
                      $"/ 关键词={string.Join("、", structured.Keywords ?? [])}");
}
else if (raw is not null)
{
    Console.WriteLine($"⚠️ 降级：FinishReason={reason}");
    Console.WriteLine($"   RawText：{raw[..Math.Min(150, raw.Length)]}...");
}
else
{
    Console.WriteLine($"❌ 完全失败：reason={reason}");
}


static bool IsRetryable(Exception ex) => ex switch
{
    // 传输层：网络不通 / 连接被拒 → 不可重试（除非是 5xx 级别的传输错误）
    HttpRequestException hre => hre.StatusCode is HttpStatusCode.TooManyRequests
                                   or HttpStatusCode.InternalServerError
                                   or HttpStatusCode.BadGateway
                                   or HttpStatusCode.ServiceUnavailable,
    // 应用层：检查 Status（int 类型）
    // 429 = 限流 → 可重试；5xx = 服务器错误 → 可重试；401/400/403 → 不可重试
    ClientResultException cre => cre.Status is 429 or >= 500 and <= 599,
    // 429 = 限流 → 可重试；5xx = 服务器错误 → 可重试；401/400/403 → 可重试
    // ClientResultException cre => cre.Status is 429 or 401 or 400 or 403 or >= 500 and <= 599,
    TaskCanceledException   => false,
    _                        => false
};


// ---------- 异常分类 ----------
static void ClassifyException(Exception ex)
{
    string category = ex switch
    {
        HttpRequestException hre => $"HTTP {hre.StatusCode}（HttpRequestException）",
        ClientResultException cre  => $"API 错误（ClientResultException）",
        TaskCanceledException      => "超时（TaskCanceledException）",
        JsonException              => "JSON 解析失败（JsonException）",
        InvalidOperationException  => "业务异常（InvalidOperationException）",
        _                          => $"其他（{ex.GetType().Name}）"
    };

    bool retryable = ex switch
    {
        HttpRequestException hre => hre.StatusCode is HttpStatusCode.TooManyRequests
                                       or HttpStatusCode.InternalServerError
                                       or HttpStatusCode.BadGateway
                                       or HttpStatusCode.ServiceUnavailable,
        TaskCanceledException    => true,   // 网络超时可重试；业务超时不可
        ClientResultException    => false,  // 通常是 400/401/402
        _                        => false
    };

    Console.WriteLine($"  类型：{category}");
    Console.WriteLine($"  可重试：{(retryable ? "是 ✅" : "否 ❌")}");
    Console.WriteLine($"  消息：{ex.Message[..Math.Min(120, ex.Message.Length)]}");
}


// 输出契约（沿用 Day006-Day008）
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

public class FewShotExample
{
    public string Review { get; set; } = "";
    public ReviewAnalysis Output { get; set; } = new();
}
