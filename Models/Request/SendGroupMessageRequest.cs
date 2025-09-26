using System.Collections.Generic;
using System.Text.Json.Serialization;
using qqbot.Models.Messages;

namespace qqbot.Models.Request;
public class SendGroupMessageRequest
{
    [JsonPropertyName("group_id")]
    public long GroupId { get; set; }

    [JsonPropertyName("message")]
    public List<MessageSegment> Message { get; set; } = new();
}