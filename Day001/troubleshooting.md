# Day 01 问题记录

## Q1：User Secrets 设置之后为什么 appsettings.json 找不到？

### 现象

...

### 我最开始的理解

我以为：

dotnet user-secrets set
    ↓
appsettings.json

### 实际情况

...

### 排查

1. 执行 dotnet user-secrets list
2. 检查 .csproj
3. 查看 UserSecretsId
4. 验证 IConfiguration

### 根因

...

### 解决

...

### 最终理解

...

### 面试表达

...


## Q2：第一天调用 OpenAI 模型成功，第二天调用失败：

### 现象

2026-09-02
    ↓
创建 key01
    ↓
Day001 HelloAI
    ↓
正常调用 OpenAI
    ↓
2026-09-03
    ↓
key01 仍然存在
    ↓
但 Status 自动变成 Inactive
    ↓
HelloAI 开始出现
401 invalid_api_key

### 排查

1. 代码未作任何修改
2. 检查 User Secrets，中的配置正确
3. openai 后台的 Settings>Limits中的：Model Usage 里的配置正确、
4. 查看openai后台个人账户billing中的余额为0，但是如果是余额问题，返回的应该不是401 invalid_api_key，应该是类似 credit_balance_exhausted 的余额不足的提示
5. 登录 OpenAI 控制台，确认 key01 仍然存在，但是 Status 已变成 Inactive，但是没有做个任何操作，自动变成这样

### 根因

1. openai官方约定，key的内容禁止透露到公网中，包括git提交到github等平台
2. 排查后发现在 00-学习说明/ 中，之前提交了一个word文档，其中包含了 key01 的内容，导致 key01 被泄露到公网中

### 解决

1. 从 00-学习说明/ 中删除 key01 相关的内容
2. 重新提交项目到github
3. 新建一个key01，用于后续的调用 OpenAI 模型


### 最终理解

key的值被暴露到公网中，导致被泄露，从而导致调用失败

### 面试表达

...