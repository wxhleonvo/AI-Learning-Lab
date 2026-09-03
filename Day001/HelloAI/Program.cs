using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();
string? model = config["ModelName"];
string? key = config["OpenAIKey"];


IChatClient client =
    new OpenAIClient(key).GetChatClient(model).AsIChatClient();

string text = File.ReadAllText("benefits.md");
string prompt = $"""
    Summarize the the following text in 20 words or less:
    {text}
    """;

// prompt = "请用一句话解释 RAG";
prompt = "请用一句话解释什么是人工智能。";
prompt = "请用一个生活中的例子解释什么是人工智能。";
prompt = @"你是一名面向初学者的技术老师。

请用一个生活中的例子解释什么是人工智能。
不要使用复杂的专业术语。";

// Submit the prompt and print out the response.
ChatResponse response = await client.GetResponseAsync(
    prompt,
    new ChatOptions { MaxOutputTokens = 400 });
Console.WriteLine(response);