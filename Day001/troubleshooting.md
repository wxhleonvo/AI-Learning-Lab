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