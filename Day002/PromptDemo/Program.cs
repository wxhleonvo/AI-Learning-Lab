using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();
string? model = config["ModelName"];
string? key = config["OpenAIKey"];

// 应用程序与聊天模型交互的抽象接口，不是模型本身。
IChatClient client =
    new OpenAIClient(key).GetChatClient(model).AsIChatClient();


/*
// 实验一：Plain Prompt
string prompt = @"请把下面的中文翻译成英文：

今天天气很好。";
// Submit the prompt and print out the response.
// ChatResponse 模型调用返回的响应对象。
// GetResponseAsync 发送 Prompt/消息并异步得到模型响应。
ChatResponse response = await client.GetResponseAsync(prompt);
// 模型调用返回的响应对象。
Console.WriteLine(response);
*/

/*
// 实验二：System + User
List<ChatMessage> messages =
[
    new(ChatRole.System, """
        你是一名专业的中英翻译助手。
        只输出英文翻译，不要解释。
        """),

    new(ChatRole.User, """
        今天天气很好。
        """)
];

ChatResponse response =
    await client.GetResponseAsync(messages);

Console.WriteLine(response.Text);
*/

/*
// 实验三：System + User
List<ChatMessage> messages =
[
    new(ChatRole.System, """
        你是一名英语老师。
        请翻译中文，并在翻译之后解释关键语法。
        """),

    new(ChatRole.User, """
        今天天气很好。
        """)
];
ChatResponse response =
    await client.GetResponseAsync(messages);
Console.WriteLine(response.Text);
*/

List<ChatMessage> messages1 =
[
    new(ChatRole.System, """
        你是一名教育专家。
        请帮我用简洁、通俗易懂的语言解释一下。
        """),

    new(ChatRole.User, """
        母题解法
        """),
    new(ChatRole.User, """
        费曼学习法
        """)
];

foreach (var message in messages1)
{
    Console.WriteLine($"{message.Role}: {message.Text}");
}

ChatResponse response1 =
    await client.GetResponseAsync(messages1);
Console.WriteLine(response1.Text);

