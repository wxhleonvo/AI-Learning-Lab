# Day 01 学习笔记

## 1. LLM 调用链

C# > OpenAi LLM > Response

## 2. User Secrets

### dotnet user-secrets init

它主要是在你的 .csproj 中加入一个 UserSecretsId：
<PropertyGroup>
  <UserSecretsId>xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx</UserSecretsId>
</PropertyGroup>
这个 ID 用来告诉 .NET：
“这个项目应该去哪个 User Secrets 存储区找配置。”

### dotnet user-secrets set

dotnet user-secrets set OpenAIKey abc123
dotnet user-secrets set ModelName gpt-xxx
实际上会保存成类似：
{
  "OpenAIKey": "abc123",
  "ModelName": "gpt-xxx"
}
但这个 JSON 不在你的项目目录里。

Windows 下一般在：

%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json

也就是类似：

C:\Users\你的用户名\AppData\Roaming\Microsoft\UserSecrets\
    xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx\
        secrets.json

所以你在：

Day01.HelloAI/
├── Program.cs
├── Day01.HelloAI.csproj
├── appsettings.json
└── ...

里面找不到它。

这是故意设计成这样的。


### secrets 到底存在哪里

...

## 3. IConfiguration

那程序为什么能读到？

这正是 .NET Configuration 系统比较重要的一个概念。

开发环境下，程序可以把多个配置来源合并起来：

appsettings.json
        ↓
appsettings.Development.json
        ↓
Environment Variables
        ↓
User Secrets
        ↓
程序最终得到 IConfiguration

所以代码可能这样读取：

var builder = Host.CreateApplicationBuilder(args);

var apiKey = builder.Configuration["OpenAIKey"];
var modelName = builder.Configuration["ModelName"];

虽然：

appsettings.json

里面没有：

"OpenAIKey": "..."

但是：

builder.Configuration["OpenAIKey"]

仍然能够拿到值。

因为这个值来自 User Secrets。

# 可以自己验证一下

在项目目录执行：

dotnet user-secrets list

应该能看到：

OpenAIKey = ...
ModelName = ...

注意：如果终端直接显示真实 API Key，不要把这个输出截图发到 GitHub、群聊或者聊天窗口。

你还可以执行：

dotnet user-secrets clear

但现在先不要执行，否则会把你刚配置的 Secret 删除。



## 4. 今天我的理解

以前我认为：

...

现在理解：

...

## 5. 还没搞懂

- Production 环境 Secret 应该怎么管理？
- Azure Key Vault 怎么接入？

## 6. 面试表达

如果面试官问：

> ASP.NET Core 的配置是从哪里来的？

我的回答：

...