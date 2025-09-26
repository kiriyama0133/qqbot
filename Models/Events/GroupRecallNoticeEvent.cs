using System.Text.Json.Serialization;

namespace qqbot.Models.Events;

/// <summary>
/// 代表群消息撤回的通知事件
/// </summary>
public class GroupRecallNoticeEvent
{
    [JsonPropertyName("post_type")]
    public string PostType { get; set; } = "notice";

    [JsonPropertyName("notice_type")]
    public string NoticeType { get; set; } = "group_recall";

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("self_id")]
    public long SelfId { get; set; }

    [JsonPropertyName("group_id")]
    public long GroupId { get; set; }

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("operator_id")]
    public long OperatorId { get; set; }

    [JsonPropertyName("message_id")]
    public long MessageId { get; set; }
}
