## 请求异常梳理

异常	                来源	              什么时候抛	                          本质
HttpRequestException	System.Net.Http	    网络层：DNS 解析失败、连接被拒、传输超时	HTTP 请求本身没成功（连不上服务器）
ClientResultException	System.ClientModel	应用层：服务器收到请求并返回了错误状态码	 HTTP 请求成功完成，但服务器说"不行"

# 401 场景示例【返回的错误是 ClientResultException，原先以为是属于 HttpRequestException 范畴】：

异常提示：Unhandled exception. System.ClientModel.ClientResultException: HTTP 401 (invalid_request_error: invalid_api_key)

你的程序 → HttpClient → 网络（通的）→ DashScope 服务器
                                        ↓
                              "API Key 无效！" → 返回 HTTP 401
                                        ↓
                              OpenAI SDK 解析 401 响应体
                                        ↓
                              抛出 ClientResultException
                              （带状态码 401 + 错误详情）

网络是通的，HTTP 事务成功完成了——服务器收到了请求、处理了、返回了 401 响应。所以这不是 HttpRequestException（传输层没出问题），而是 ClientResultException（应用层收到了错误响应）。
HttpRequestException 只在根本连不上服务器时才会出现（断网、域名不存在、端口没开）。
