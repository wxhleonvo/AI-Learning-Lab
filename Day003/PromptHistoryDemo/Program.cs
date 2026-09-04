﻿using Microsoft.Extensions.AI;
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
List<ChatMessage> history =
[
    new(ChatRole.System, """
        你是一个简洁、友好的中文助手。
        """)
];

// 第一轮
history.Add(new(ChatRole.User, "我叫张三。"));

ChatResponse response1 =
    await client.GetResponseAsync(history);

Console.WriteLine($"AI: {response1.Text}");

// 把 AI 回复加入历史
history.AddMessages(response1);

// 请求前输出当前历史
Console.WriteLine("===== 第一轮请求后，当前 History =====");
foreach (var message in history)
{
    Console.WriteLine($"{message.Role}: {message.Text}");
}
Console.WriteLine("==================================");

// 第二轮
history.Add(new(ChatRole.User, "我叫什么？"));

ChatResponse response2 =
    await client.GetResponseAsync(history);

Console.WriteLine($"AI: {response2.Text}");

// 把 AI 回复加入历史
history.AddMessages(response2);

// 请求前输出当前历史
Console.WriteLine("===== 第二轮请求后，当前 History =====");
foreach (var message in history)
{
    Console.WriteLine($"{message.Role}: {message.Text}");
}
Console.WriteLine("==================================");
*/


List<ChatMessage> history =
[
    new(ChatRole.System, """
        你是一个简洁、友好的中文 AI 助手。
        请根据当前对话历史回答用户。
        """)
];

while (true)
{
    Console.Write("你：");
    string? input = Console.ReadLine();

    if (input is null) break; // 输入流结束（Ctrl+Z 或管道 EOF），避免死循环

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (input == "/history")
    {
        Console.WriteLine("===== Chat History =====");

        foreach (var message in history)
        {
            Console.WriteLine(
                $"[{message.Role}] {message.Text}");
        }

        Console.WriteLine("========================");

        continue;
    }

    history.Add(new(ChatRole.User, input));

    ChatResponse response =
        await client.GetResponseAsync(history);

    Console.WriteLine($"AI：{response.Text}");

    history.AddMessages(response);
}