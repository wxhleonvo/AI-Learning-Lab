## 【示例代码语法问题】 9. 实验二：One-shot（+1 个示例）

# 现象：提交大模型后报错：
G:\AI学习\AI-Learning-Lab\Day008\FewShotDemo\Program.cs(81,8): error CS9006: 内插原始字符串字面量的开头没有足够的 \"$\" 字符以允许将这么多连续的左大括号作为内容。
G:\AI学习\AI-Learning-Lab\Day008\FewShotDemo\Program.cs(81,10): error CS1733: 应为表达式

# 原因：
问题根因：C# 11 内插原始字符串的规则——

$"""..."""（1 个 $）：单个 { 开启插值，{{ 无法作为字面量（编译器报 CS9006）
$$"""..."""（2 个 $）：单个 { } 是字面量，插值需 {{expr}}

# 解决：
示例 JSON 的 {{...}} 转义改回自然的 {...}（更易读）
{targetReview} → {{targetReview}}
Few-shot 示例里有 JSON 字面量大括号时，优先用 $$"""..."""，让 JSON 写成自然形式，插值用 {{}}。后续 Three-shot / Five-shot / Conflict 的示例段也建议照此改写。

## 【请求输出无结果】 9. 实验二：One-shot（+1 个示例）

# 参数1：ChatOptions { Temperature = 0.1f, MaxOutputTokens = 1400 } 输出：解析失败，原始：
# 参数2：ChatOptions { Temperature = 0.1f, MaxOutputTokens = 4400 } 输出：正常解析。 应该是提示词Prompt内容长，大模型在思考的时候输出了更多的内容，导致输出token数量超过了MaxOutputTokens。


## Recency Bias（近因偏差） 
# 现象：
提示词中的示例代码最后的示例内容，会被模型优先考虑(并非肯定会影响模型输出)，模型更受“最后看到的示例”影响。
# 注意：不同模型对提示词的解析不同，导致Recency Bias表现不同。
