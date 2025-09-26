using System.Collections.Generic;
using System.Text.Json.Serialization;
using qqbot.Models.Events;

namespace qqbot.Models.Response;

/// <summary>
/// 代表"获取群消息历史"API的完整响应体
/// </summary>
public class GetGroupMessageHistoryResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("retcode")]
    public int ReturnCode { get; set; }

    [JsonPropertyName("data")]
    public MessageHistoryData? Data { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("wording")]
    public string Wording { get; set; } = string.Empty;

    [JsonPropertyName("echo")]
    public string? Echo { get; set; }
}

/// <summary>
/// 响应体中 "data" 字段对应的结构
/// </summary>
public class MessageHistoryData
{
    [JsonPropertyName("messages")]
    public List<GroupMessageEvent> Messages { get; set; } = new();
}
