# Token 是什么？
 Token 是一个单位，大模型处理文字时的最小单位，即不是字，也不是词，而是每个模型自认为应该放在一起的一小块文字。
 一小块 = 1个 Token
 
# 怎么分词？即怎么认定为一小块文字
 每个模型都有自己的分词规则，根据模型的训练数据和任务类型，将文字分割成 Token。

# Context Window 是什么？
 Context Window 是大模型处理文字时，用于存储上下文的 Token 数量。
 Context Window = 输入 Token 数量（包括：System Prompt + User Prompt） + 输出 Token 数量(包括：模型思考 + 输出内容)