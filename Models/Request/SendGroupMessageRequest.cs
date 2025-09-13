using System.Text.Json.Serialization;
namespace qqbot.Models.Request;
public class SendGroupMessageRequest
{
    [JsonPropertyName("group_id")]
    public long GroupId { get; set; }

    [JsonPropertyName("message")]
    public List<MessageSegment> Message { get; set; } = new();
}