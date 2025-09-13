using qqbot.Helper.HttpHandlers;
using qqbot.Models;
using qqbot.Models.Request;
using qqbot.Models.Response;

namespace qqbot.Services;

public partial class NapCatApiService
{

    /// <summary>
    /// 发送私聊消息
    /// </summary>
    /// <param name="userId">对方 QQ 号</param>
    /// <param name="message">要发送的结构化消息列表</param>
    /// <returns>API的响应数据模型</returns>
    public async Task<SendMessageResponse?> SendPrivateMessageAsync(long userId, List<MessageSegment> message)
    {
        try
        {
            //  创建一个强类型的请求体 (Payload) 对象
            var payload = new SendPrivateMessageRequest
            {
                UserId = userId,
                Message = message
            };

            //    发起 HTTP POST 请求。这一步会创建并赋值 response 变量。
            //    PostAsJsonAsync 会自动将 payload 对象序列化为 JSON 字符串。
            //    我们的拦截器管道会在此调用中自动添加 Token 和日志。
            var response = await _httpClient.PostAsJsonAsync("/send_private_msg", payload);

            //    我们的 ErrorAndLoggingHandler 拦截器已经处理了失败的状态码。
            //    如果请求失败（例如 404, 500），拦截器会直接抛出 ApiException，
            //    下面的代码将不会被执行，而是直接进入 catch 块。

            //    如果请求成功，我们将响应体反序列化为我们的 C# 模型并返回。
            return await response.Content.ReadFromJsonAsync<SendMessageResponse>();
        }
        catch (ApiException ex) // 捕获由拦截器抛出的自定义 API 异常
        {
            _logger.LogError(ex, "API 请求返回了一个失败的状态码 (发送私聊消息)。");
            return null;
        }
        catch (Exception ex) // 捕获其他可能的异常 (如网络超时、JSON解析失败等)
        {
            _logger.LogError(ex, "发送私聊消息时发生未知异常。");
            return null;
        }
    }


    public async Task<SendMessageResponse?> SendGroupMessageAsync(long groupId, List<MessageSegment> message)
    {
        try
        {
            var payload = new SendGroupMessageRequest
            {
                GroupId = groupId,
                Message = message
            };

            var response = await _httpClient.PostAsJsonAsync("/send_group_msg", payload);
            return await response.Content.ReadFromJsonAsync<SendMessageResponse>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API 请求返回了一个失败的状态码 (发送群消息)。");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送群消息时发生未知异常。");
            return null;
        }
    }


}
