using System.Collections.Generic;
using System.Text.Json.Serialization;
using qqbot.Models.Entities;

namespace qqbot.Models.Response;

/// <summary>
/// 代表"获取群列表"API的完整响应体
/// </summary>
public class GetGroupListResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("retcode")]
    public int ReturnCode { get; set; }

    [JsonPropertyName("data")]
    public List<GroupInfo>? Data { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("wording")]
    public string Wording { get; set; } = string.Empty;

    [JsonPropertyName("echo")]
    public string Echo { get; set; } = string.Empty;
}
