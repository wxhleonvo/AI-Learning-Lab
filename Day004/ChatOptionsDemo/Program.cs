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

/*
// 实验1：不同 MaxOutputTokens 对输出长度的影响
string prompt = """
请解释什么是 RAG。
要求：
1. 面向刚开始学习 AI 的 C# 开发者
2. 使用中文
3. 不超过 200 字
""";

ChatResponse response =
    await client.GetResponseAsync(
        prompt,
        new ChatOptions { MaxOutputTokens = 1000 });

Console.WriteLine($"FinishReason: {response.FinishReason}");
Console.WriteLine($"InputTokens:  {response.Usage.InputTokenCount}");
Console.WriteLine($"OutputTokens: {response.Usage.OutputTokenCount}");
Console.WriteLine($"TextLength:   {response.Text?.Length}");
Console.WriteLine("---");
Console.WriteLine(response.Text);
*/

/*
// 实验2：不同 Temperature 对输出的影响的影响
string prompt = """
请写一段关于“AI程序员”的创意描述。
要求：
1. 中文
2. 100字左右
3. 有一点科幻感
""";

ChatResponse response1 =
    await client.GetResponseAsync(
        prompt,
        new ChatOptions { Temperature = 0.0f });
Console.WriteLine(response1.Text);

ChatResponse response2 =
    await client.GetResponseAsync(
        prompt,
        new ChatOptions { Temperature = 0.3f });
Console.WriteLine(response2.Text);

ChatResponse response3 =
    await client.GetResponseAsync(
        prompt,
        new ChatOptions { Temperature = 0.7f });
Console.WriteLine(response3.Text);

ChatResponse response4 =
    await client.GetResponseAsync(
        prompt,
        new ChatOptions { Temperature = 1.0f });
Console.WriteLine(response4.Text);
*/

/*
// 实验3：同 Temperature 测试多次， 对输出的影响的影响
string prompt = """
请写一段关于“AI程序员”的创意描述。
要求：
1. 中文
2. 100字左右
3. 有一点科幻感
""";
int times = 0;
while (times<5)
{
    ChatResponse response3 =
    await client.GetResponseAsync(
        prompt,
        new ChatOptions { Temperature = 1.0f });
    Console.WriteLine( $"temp=1.0 第{times}次：{ response3.Text}");
    times++;
}
*/

/*
// 实验4：TopP 对输出的影响的影响
string prompt = """
每一个人都有一个唯一性吗？
要求：
1. 中文
2. 不超过 20 字
""";
float times = 0.0f;
while (times<=1.0f)
{
    ChatResponse response3 =
    await client.GetResponseAsync(
        prompt,
        new ChatOptions { TopP = times });
    Console.WriteLine( $"TopP={times}：{ response3.Text}");
    times += 0.1f;
}
*/

/*
// 实验5：StopSequences 对输出的影响
string prompt = """
请输出三个关键词，每个关键词一行。
最后输出 END。
""";

// 对照组：不设 StopSequences
ChatResponse responseNoStop =
    await client.GetResponseAsync(prompt, new ChatOptions());
Console.WriteLine("—— 未设 StopSequences ——");
Console.WriteLine($"FinishReason: {responseNoStop.FinishReason}");
Console.WriteLine($"Text:\n{responseNoStop.Text}");
Console.WriteLine();

// 实验组：设 StopSequences = ["END"]
ChatResponse responseWithStop =
    await client.GetResponseAsync(
        prompt,
        new ChatOptions { StopSequences = ["END"] });
Console.WriteLine("—— StopSequences=[\"END\"] ——");
Console.WriteLine($"FinishReason: {responseWithStop.FinishReason}");
Console.WriteLine($"Text:\n{responseWithStop.Text}");
*/

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

string prompt = """
请输出三个关键词，每个关键词一行。
最后输出 END。
""";

ChatResponse response =
    await client.GetResponseAsync(
        prompt,
        optionsConfig);
Console.WriteLine($"{response.Text}");        
