using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace qqbot.Models.Request;

// 这个模型将用于构建发送给 NapCat 的 JSON 请求体
public class SendPrivateMessageRequest
{
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("message")]
    public List<MessageSegment> Message { get; set; } // 消息内容可以是结构化的
}