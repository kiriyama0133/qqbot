using System.Collections.Generic;
using System.Text.Json.Serialization;
using qqbot.Models.Messages;

namespace qqbot.Models.Events;

/// <summary>
/// 代表从 NapCat/OneBot 接收到的实时群聊消息事件
/// </summary>
public class GroupMessageEvent
{
    [JsonPropertyName("post_type")]
    public string PostType { get; set; } = "message";

    [JsonPropertyName("message_type")]
    public string MessageType { get; set; } = "group";

    [JsonPropertyName("sub_type")]
    public string SubType { get; set; } = string.Empty;

    [JsonPropertyName("message_id")]
    public int MessageId { get; set; }

    [JsonPropertyName("group_id")]
    public long GroupId { get; set; }

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("sender")]
    public Entities.SenderInfo? Sender { get; set; }

    [JsonPropertyName("message")]
    public List<MessageSegment> Message { get; set; } = new();

    [JsonPropertyName("raw_message")]
    public string RawMessage { get; set; } = string.Empty;

    [JsonPropertyName("font")]
    public int Font { get; set; }

    [JsonPropertyName("self_id")]
    public long SelfId { get; set; }

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("echo")]
    public string? Echo { get; set; }
}
