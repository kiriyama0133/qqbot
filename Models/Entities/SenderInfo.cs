using System.Text.Json.Serialization;

namespace qqbot.Models.Entities;

/// <summary>
/// 发送者信息
/// </summary>
public class SenderInfo
{
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("card")]
    public string Card { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}
