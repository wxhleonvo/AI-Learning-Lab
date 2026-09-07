## 请求大模型时，参数配置，常见问题

 ChatResponse<ReviewAnalysis> response =
        await client.GetResponseAsync<ReviewAnalysis>(
            reviewPrompt,
            new ChatOptions { Temperature = 0.1f });

# 1. 当使用大模型  kimi-k3 时，返回的模型输出报错：
Unhandled exception. System.ClientModel.ClientResultException: HTTP 400 (invalid_request_error: invalid_parameter_error)
<400> InternalError.Algo.InvalidParameter: Parameter 'temperature'=0.1 is not supported for kimi-k3 model.

# 2. 更换其他模型比如：glm-5.2，qwen3.8-max-0902 等支持的模型即可。


## 相同的评论，使用不同的模型时，返回的模型输出不同（glm-5.2 和 qwen3.8-max-0902 和 qwen3.8-max，发现后两者精度相对高些，具体还需针对需求进行模拟测试）

# 评论：
string[] reviews =
[
    "这个产品太好用了！物流第二天就到，客服也很耐心，强烈推荐。",
    "质量很差，用了两天就坏了，客服还不理人，非常失望。",
    "东西收到了，包装完好，目前用着还行，以后再追评。"
];

# 定义情感倾向枚举1：
public enum Sentiment
{
    Positive,   // 正面
    Negative,   // 负面
    Neutral     // 中性
}

public class ReviewAnalysis
{
    [Description("情感倾向，只能是 Positive、Negative、Neutral 之一")]
    public Sentiment Sentiment { get; set; }

    [Description("0 到 10 的情感强度评分，整数")]
    public int Score { get; set; }

    [Description("一句话中文结论")]
    public string? Conclusion { get; set; }
}

# 返回结果1：
# glm-5.2
 论：这个产品太好用了！物流第二天就到，客服也很耐心，强烈推荐。
  → 情感：Positive，评分：5/10，结论：强烈推荐该产品，整体体验极佳。

评论：质量很差，用了两天就坏了，客服还不理人，非常失望。
  → 情感：Negative，评分：1/10，结论：差评/非常失望，质量极差且售后不作为，不推荐购买。

评论：东西收到了，包装完好，目前用着还行，以后再追评。
  → 情感：Neutral，评分：3/10，结论：目前使用体验尚可，但缺乏深度评价，需等待后续追评确认最终质量。
  
 # qwen3.8-max-0902
 评论：这个产品太好用了！物流第二天就到，客服也很耐心，强烈推荐。
  → 情感：Positive，评分：10/10，结论：用户对产品的使用体验、物流速度和客服服务都非常满意，并强烈推荐。

评论：质量很差，用了两天就坏了，客服还不理人，非常失望。
  → 情感：Negative，评分：9/10，结论：用户对产品质量和客服服务非常不满，表达了强烈的失望情绪。

评论：东西收到了，包装完好，目前用着还行，以后再追评。
  → 情感：Neutral，评分：5/10，结论：用户收到商品且包装完好，目前使用体验一般，暂未形成明确评价。
 
 # qwen3.8-max
 评论：这个产品太好用了！物流第二天就到，客服也很耐心，强烈推荐。
  → 情感：Positive，评分：10/10，结论：用户对产品质量、物流速度和客服服务都非常满意，并强烈推荐。

评论：质量很差，用了两天就坏了，客服还不理人，非常失望。
  → 情感：Negative，评分：9/10，结论：用户对产品质量和客服服务非常失望。

评论：东西收到了，包装完好，目前用着还行，以后再追评。
  → 情感：Neutral，评分：5/10，结论：用户收到商品且包装完好，目前使用体验一般，态度中性并计划后续追评。
  

# 定义情感倾向枚举2(Score的描述更加详细)：
public enum Sentiment
{
    Positive,   // 正面
    Negative,   // 负面
    Neutral     // 中性
}

public class ReviewAnalysis
{
    [Description("情感倾向，只能是 Positive、Negative、Neutral 之一")]
    public Sentiment Sentiment { get; set; }

    [Description("0 到 10 的情感强度评分，整数, Sentiment 为 Positive 时，Score 越高，Negative 时，Score 越低")]
    public int Score { get; set; }

    [Description("一句话中文结论")]
    public string? Conclusion { get; set; }
}

# 返回结果2：

# glm-5.2
评论：这个产品太好用了！物流第二天就到，客服也很耐心，强烈推荐。
  → 情感：Positive，评分：5/10，结论：强烈推荐

评论：质量很差，用了两天就坏了，客服还不理人，非常失望。
  → 情感：Negative，评分：1/10，结论：非常失望，产品质量差且售后服务不佳，客户体验极差，建议改进产品质量和客服响应速度。

评论：东西收到了，包装完好，目前用着还行，以后再追评。
  → 情感：Neutral，评分：3/10，结论：目前满意，待观察

# qwen3.8-max-0902
评论：这个产品太好用了！物流第二天就到，客服也很耐心，强烈推荐。
  → 情感：Positive，评分：10/10，结论：用户对产品的使用体验、物流速度和客服服务都非常满意，并强烈推荐。

评论：质量很差，用了两天就坏了，客服还不理人，非常失望。
  → 情感：Negative，评分：1/10，结论：产品质量差且很快损坏，客服无回应，用户非常失望。

评论：东西收到了，包装完好，目前用着还行，以后再追评。
  → 情感：Neutral，评分：5/10，结论：用户对商品包装和初步使用体验表示一般满意，暂无明显正面或负面评价。

# qwen3.8-max
评论：这个产品太好用了！物流第二天就到，客服也很耐心，强烈推荐。
  → 情感：Positive，评分：10/10，结论：用户对产品的使用体验、物流速度和客服服务都非常满意，并强烈推荐。

评论：质量很差，用了两天就坏了，客服还不理人，非常失望。
  → 情感：Negative，评分：1/10，结论：产品质量差且客服响应不佳，用户非常失望。

评论：东西收到了，包装完好，目前用着还行，以后再追评。
  → 情感：Neutral，评分：5/10，结论：用户收到商品且包装完好，目前使用感受一般，态度中性，有待后续追评。
