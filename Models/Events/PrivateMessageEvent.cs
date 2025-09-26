using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using qqbot.Models.Messages;

namespace qqbot.Models.Events;

/// <summary>
/// 代表从 NapCat/OneBot 接收到的私聊消息事件
/// </summary>
public class PrivateMessageEvent
{
    [JsonPropertyName("self_id")]
    public long SelfId { get; set; }

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("message_id")]
    public long MessageId { get; set; }

    [JsonPropertyName("message_seq")]
    public long MessageSequence { get; set; }

    [JsonPropertyName("real_id")]
    public long RealId { get; set; }

    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = string.Empty;

    [JsonPropertyName("sender")]
    public Entities.SenderInfo? Sender { get; set; }

    [JsonPropertyName("raw_message")]
    public string RawMessage { get; set; } = string.Empty;

    [JsonPropertyName("font")]
    public int Font { get; set; }

    [JsonPropertyName("sub_type")]
    public string SubType { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public List<MessageSegment> Message { get; set; } = new();

    [JsonPropertyName("message_format")]
    public string MessageFormat { get; set; } = string.Empty;

    [JsonPropertyName("post_type")]
    public string PostType { get; set; } = string.Empty;

    [JsonPropertyName("target_id")]
    public long TargetId { get; set; }

    [JsonPropertyName("raw")]
    public JsonElement? Raw { get; set; }
}
