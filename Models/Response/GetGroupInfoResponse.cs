using System.Text.Json.Serialization;
using qqbot.Models.Entities;

namespace qqbot.Models.Response;

/// <summary>
/// 代表"获取群信息"API的完整响应体
/// </summary>
public class GetGroupInfoResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("retcode")]
    public int ReturnCode { get; set; }

    [JsonPropertyName("data")]
    public GroupInfo? Data { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("wording")]
    public string Wording { get; set; } = string.Empty;

    [JsonPropertyName("echo")]
    public string? Echo { get; set; }
}
