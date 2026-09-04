## 对话时，AI LLM 返回乱码提示，效果如下：

你：我叫张三
AI：你好！我在这里，可以帮你回答问题、写作、翻译、总结、代码等。  
如果你现在只是想测试一下，我也收到了。你可以直接告诉我你的需求。  
你：我喜欢C#
AI：看起来你发来的内容有乱码（`���C#`），可能是编码问题。

排查后原因如下：
// ===== 中文控制台输入乱码修复 =====
// 现象：.NET 10 默认用 UTF-8 解码控制台输入，而中文 Windows 控制台（代码页 936）
// 送进来的是 GBK 字节，GBK 字节按 UTF-8 解码会得到 U+FFFD 乱码发给大模型。
// 修复：注册 GBK 等代码页支持（net10.0 已内置，无需额外包），让解码方式与控制台字节一致。
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.InputEncoding = Encoding.GetEncoding(936);
// 备选方案：先在终端执行 chcp 65001 把控制台切到 UTF-8，然后本行改为
// Console.InputEncoding = Encoding.UTF8;

## 对话时，AI LLM 返回 HTTP 429 错误，效果如下：
Unhandled exception. System.ClientModel.ClientResultException: HTTP 429 (insufficient_quota: credit_balance_exhausted)

You have no credits remaining. Add credits to continue using the API at https://platform.openai.com/settings/organization/billing/.
   at OpenAI.ClientPipelineExtensions.ProcessMessageAsync(ClientPipeline pipeline, PipelineMessage message, RequestOptions options)
   at OpenAI.Chat.ChatClient.CompleteChatAsync(BinaryContent content, RequestOptions options)
   at OpenAI.Chat.ChatClient.CompleteChatAsync(IEnumerable`1 messages, ChatCompletionOptions options, RequestOptions requestOptions)
   at Microsoft.Extensions.AI.OpenAIChatClient.GetResponseAsync(IEnumerable`1 messages, ChatOptions options, CancellationToken cancellationToken)
   at Program.<Main>$(String[] args) in G:\AI学习\AI-Learning-Lab\Day003\PromptHistoryDemo\Program.cs:line 115
   at Program.<Main>(String[] args)

# 原因：
账号余额不足，无法继续调用 API。

# 解决：
暂时修改成阿里云百炼模型，继续调用 API。
cd g:\AI学习\AI-Learning-Lab\Day003\PromptHistoryDemo
dotnet user-secrets set "DashScopeKey" "sk-你的百炼API密钥"
dotnet user-secrets set "DashScopeModel" "kimi-k3"

改动说明：
NuGet 包不变 — 百炼兼容 OpenAI 接口，继续用 OpenAIClient
Program.cs#L22-25：新增 OpenAIClientOptions，把 endpoint 指向百炼兼容地址 https://dashscope.aliyuncs.com/compatible-mode/v1
Program.cs#L30：key 用 ApiKeyCredential 包装（带 options 参数的重载要求该类型）
配置项从 ModelName/OpenAIKey 改为 DashScopeModel/DashScopeKey
常用的百炼模型名称：qwen-plus、qwen-turbo、qwen-max、qwen-long 等，按需填写。