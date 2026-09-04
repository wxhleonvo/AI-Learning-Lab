using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;
using System.Text;

// ===== 中文控制台输入乱码修复 =====
// 现象：.NET 10 默认用 UTF-8 解码控制台输入，而中文 Windows 控制台（代码页 936）
// 送进来的是 GBK 字节，GBK 字节按 UTF-8 解码会得到 U+FFFD 乱码发给大模型。
// 修复：注册 GBK 等代码页支持（net10.0 已内置，无需额外包），让解码方式与控制台字节一致。
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.InputEncoding = Encoding.GetEncoding(936);
// 备选方案：先在终端执行 chcp 65001 把控制台切到 UTF-8，然后本行改为
// Console.InputEncoding = Encoding.UTF8;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();
string? model = config["DashScopeModel"];//阿里云百炼模型
string? key = config["DashScopeKey"];//阿里云百炼 API 密钥

// 阿里云百炼（DashScope）兼容 OpenAI 接口，只需把 Endpoint 指向百炼地址
OpenAIClientOptions options = new()
{
    Endpoint = new Uri("https://dashscope.aliyuncs.com/compatible-mode/v1")
};

// 应用程序与聊天模型交互的抽象接口，不是模型本身。
IChatClient client =
    new OpenAIClient(new ApiKeyCredential(key!), options).GetChatClient(model).AsIChatClient();

// 实验九：把参数放进配置文件
IConfigurationRoot configXml =
    new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddUserSecrets<Program>()
        .Build();

int maxOutputTokens =
    configXml.GetValue<int>("AI:MaxOutputTokens");

float temperature =
    configXml.GetValue<float>("AI:Temperature");

float topP =
    configXml.GetValue<float>("AI:TopP");

ChatOptions optionsConfig = new()
{
    MaxOutputTokens = maxOutputTokens,
    Temperature = temperature,
    TopP = topP
};

/*
// 实验一：普通响应
ChatResponse response = await client.GetResponseAsync(
    "请用中文解释什么是 RAG，并举一个企业知识库的例子。",
    optionsConfig);

Console.WriteLine("=== 完整响应 ===");
Console.WriteLine(response.Text);
*/

/*
// 实验二：第一次 Streaming： 观察 内容是否逐步出现
Console.WriteLine("=== 流式响应 ===");
await foreach (ChatResponseUpdate update in 
    client.GetStreamingResponseAsync(
    "请用中文解释什么是 RAG，并举一个企业知识库的例子。",
    new ChatOptions
    {
        // MaxOutputTokens = 300,
        Temperature = 0.3f
    })
)
{
    Console.Write(update);
}

Console.WriteLine();
*/

/*
// 实验三：观察 ChatResponseUpdate
Console.WriteLine("=== 流式响应 ===");
await foreach (ChatResponseUpdate update in 
    client.GetStreamingResponseAsync(
    "请用中文解释什么是 RAG，并举一个企业知识库的例子。",
    new ChatOptions
    {
        // MaxOutputTokens = 300,
        Temperature = 0.3f
    })
)
{
    Console.WriteLine("----- UPDATE -----");
    Console.WriteLine($"Role: {update.Role}");
    Console.WriteLine($"Text: {update.Text}");
}

Console.WriteLine();
*/

/*
// 实验四：自己拼完整答案
Console.WriteLine("=== 流式响应 ===");
string fullText = "";
await foreach (ChatResponseUpdate update in 
    client.GetStreamingResponseAsync(
    "请用中文解释什么是 RAG，并举一个企业知识库的例子。不要超过300个字。",
    new ChatOptions
    {
        // MaxOutputTokens = 300,
        Temperature = 0.3f
    })
)
{
    // Console.WriteLine($"{update.Text}");
    Console.Write(update); // 直接打印更新内容，不换行，由大模型自己换行
    // Console.WriteLine(update); // 打印更新内容，换行
    fullText += update.Text; // 累加更新内容，得到完整响应
}

Console.WriteLine();
Console.WriteLine($"=== 完整响应 ===");
Console.WriteLine(fullText);
*/

/*
// 实验五：统计 update 数量
int updateCount = 0;
Console.WriteLine("=== 流式响应 ===");
string fullText = "";
await foreach (ChatResponseUpdate update in 
    client.GetStreamingResponseAsync(
    "请详细解释 RAG 的工作流程，包括文档、Chunk、Embedding、向量检索和生成。",
    new ChatOptions
    {
        // MaxOutputTokens = 500,
        Temperature = 0.3f
    })
)
{
    updateCount++;
    Console.Write($"{update.Text}");
    // Console.Write(update); // 直接打印更新内容，不换行，由大模型自己换行
    // Console.WriteLine(update); // 打印更新内容，换行
    fullText += update.Text; // 累加更新内容，得到完整响应
}

Console.WriteLine();
Console.WriteLine();
Console.WriteLine($"Update 数量：{updateCount}");
Console.WriteLine($"最终字符数：{fullText.Length}");
*/

/*
// 实验六：普通调用 vs Streaming
string prompt = "请解释什么是向量数据库，并给出 3 个典型使用场景。";
ChatResponse response = await client.GetResponseAsync(
    prompt,
    optionsConfig);
Console.WriteLine("=== 普通调用: 发送 → 等待 → 完整响应 → 显示===");
Console.WriteLine(response.Text);

Console.WriteLine("=== 流式响应: 发送 → 等待 → update → update → update → …… → 完整响应 ===");
await foreach (ChatResponseUpdate update in 
    client.GetStreamingResponseAsync(
    prompt,
    optionsConfig)
)
{
    Console.Write(update);
}
*/

/*
// # 14. 实验八：CancellationToken 取消
using var cts = new CancellationTokenSource();
Console.WriteLine($"cts.Token={cts.Token}");
await foreach (ChatResponseUpdate update
    in client.GetStreamingResponseAsync(
        "请写一篇比较长的文章，介绍企业 RAG 系统的架构。",
        optionsConfig,
        cts.Token))
{
    Console.Write(update.Text);
}
*/

/*
// # 15. 实验九：3 秒后自动取消
using var cts = new CancellationTokenSource();
//cts.CancelAfter(TimeSpan.FromSeconds(30));
Console.WriteLine($"cts.Token={cts.Token}");
try
{
    await foreach (ChatResponseUpdate update
        in client.GetStreamingResponseAsync(
            "请写一篇比较长的文章，介绍企业 RAG 系统的架构。",
            optionsConfig,
            cts.Token))
    {
        Console.Write(update.Text);
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine();
    Console.WriteLine("=== 30秒后, 请求已取消 ===");
}
*/

/*
// # 17. 实验十一：保存 Streaming 结果
string fullText = "";
string prompt = "请写一篇比较长的文章，介绍企业 RAG 系统的架构。";
await foreach (ChatResponseUpdate update
    in client.GetStreamingResponseAsync(prompt, optionsConfig))
{
    Console.Write(update.Text);
    fullText += update.Text;
}
Directory.CreateDirectory("outputs"); // 目录不存在则创建（已存在不报错）
File.WriteAllText(
    "outputs/last-response.txt",
    fullText);
Console.WriteLine();    
Console.WriteLine("=== 结果已经保存至文件中 ===");
*/

// # 18. 推荐最终版 Program.cs
string prompt = """
请详细解释企业级 RAG 系统的工作流程。
要求：
1. 使用中文。
2. 解释文档处理。
3. 解释 Chunk。
4. 解释 Embedding。
5. 解释向量检索。
6. 解释最终生成。
7. 控制在 500 字以内。
""";

ChatOptions optionsConfig2 = new()
{
    MaxOutputTokens = 500,
    Temperature = 0.3f
};

using var cts = new CancellationTokenSource();

string fullText = "";
int updateCount = 0;

Console.WriteLine("=== Streaming Start ===");

try
{
    await foreach (ChatResponseUpdate update
        in client.GetStreamingResponseAsync(
            prompt,
            optionsConfig2,
            cts.Token))
    {
        updateCount++;
        Console.Write(update.Text);
        fullText += update.Text;
    }

    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine("=== Streaming Finished ===");
    Console.WriteLine($"Update 数量：{updateCount}");
    Console.WriteLine($"最终字符数：{fullText.Length}");
}
catch (OperationCanceledException)
{
    Console.WriteLine();
    Console.WriteLine("=== Streaming Cancelled ===");
}