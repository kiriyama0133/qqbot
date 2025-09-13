using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using static qqbot.Models.Group;
using static qqbot.Models.SharedMessageModels;
namespace qqbot.Models;

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
    public SenderInfo? Sender { get; set; }

    [JsonPropertyName("raw_message")]
    public string RawMessage { get; set; } = string.Empty;

    [JsonPropertyName("font")]
    public int Font { get; set; }

    /// <summary>
    /// 消息子类型, 例如 "friend" 或 "group" (临时会话)
    /// </summary>
    [JsonPropertyName("sub_type")]
    public string SubType { get; set; } = string.Empty;

    /// <summary>
    /// 结构化的消息内容数组
    /// </summary>
    [JsonPropertyName("message")]
    public List<MessageSegment> Message { get; set; } = new();

    [JsonPropertyName("message_format")]
    public string MessageFormat { get; set; } = string.Empty;

    [JsonPropertyName("post_type")]
    public string PostType { get; set; } = string.Empty;

    /// <summary>
    /// 私聊消息的目标id，就是机器人自己的ID
    /// </summary>
    [JsonPropertyName("target_id")]
    public long TargetId { get; set; }

    /// <summary>
    /// 原始的、未被解析的事件数据，内容非常庞杂。
    /// 使用 JsonElement 可以灵活地访问，而无需为其定义一个巨大的、固定的类。
    /// </summary>
    [JsonPropertyName("raw")]
    public JsonElement? Raw { get; set; }
}