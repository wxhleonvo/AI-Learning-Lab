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

## . 三次 Prompt 实验

依次运行：

### A
```text
请用一句话解释什么是人工智能。
```

```响应：
人工智能是让计算机像人一样学习、推理和完成任务的技术。
```

### B
```text
请用一个生活中的例子解释什么是人工智能。
```

```响应：
当然可以。

**人工智能**，你可以把它理解成“让机器像人一样会思考、会判断、会做事”。
### 一个生活中的例子：手机里的语音助手
比如你对手机说：

- “帮我定个明天早上 7 点的闹钟”
- “今天北京天气怎么样？”
- “给妈妈打电话”

手机能听懂你的话，并帮你完成操作。这个过程里，手机不是简单按固定按钮工作，而是**在理解你的语言、分析你的意思，然后做出反应**。这就是人工智能的一种应用。

### 再打个比方
如果把普通机器比作“照着说明书做事的工具”，
那人工智能就像“有点聪明的助手”，它能：

- 看懂信息
- 学习规律
- 做出判断
- 帮你解决问题

### 简单总结
**人工智能就是让机器具备类似人类智能的能力，比如识别、理解、学习和决策。**

如果你愿意，我还可以用**更通俗的小朋友版**或者**再举几个生活例子**给你解释。
```



### C
```text
你是一名面向初学者的技术老师。

请用一个生活中的例子解释什么是人工智能。
不要使用复杂的专业术语。
```

```响应：
当然可以。

你可以把**人工智能**想成一个**会“学习”和“帮忙判断”的小助手**。

### 生活中的例子：教小朋友认水果
假设你有一个小朋友，刚开始他不认识苹果、香蕉和橘子。

你可以这样教他：
- 这是苹果，红红的，圆圆的。
- 这是香蕉，黄黄的，弯弯的。
- 这是橘子，橙色的，表面有点凹凸。

教了很多次以后，小朋友看到一个水果时，可能就会说：
- “这个像苹果！”
- “那个是香蕉吧！”

这时候，他不是背答案，而是**学会了自己分辨**。

### 人工智能就像什么？
人工智能就像这个学水果的小朋友，只不过它学的不是水果，而是：
- 识别图片
- 听懂说话
- 推荐你喜欢的东西
- 帮你回答问题

### 更简单地说
**人工智能就是让机器像人一样，学着看、学着听、学着判断。**

比如：
- 手机能认出你的脸
- 导航软件能帮你找路
- 视频软件会猜你想看什么

这些很多都和人工智能有关。

如果你愿意，我还可以继续用**“做饭”**或**“养宠物”**的例子再解释一遍。
```
