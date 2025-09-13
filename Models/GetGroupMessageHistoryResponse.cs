using System.Collections.Generic;
using System.Text.Json.Serialization;
using static qqbot.Models.Group;
namespace qqbot.Models;

/// <summary>
/// 代表“获取群消息历史”API的完整响应体
/// </summary>
public class GetGroupMessageHistoryResponse
{
    /// <summary>
    /// 请求状态，通常是 "ok" 或 "error"
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 响应代码
    /// </summary>
    [JsonPropertyName("retcode")]
    public int ReturnCode { get; set; }

    /// <summary>
    /// 包含消息列表的数据对象
    /// </summary>
    [JsonPropertyName("data")]
    public MessageHistoryData? Data { get; set; }

    /// <summary>
    /// 提示信息
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 提示信息（人性化）
    /// </summary>
    [JsonPropertyName("wording")]
    public string Wording { get; set; } = string.Empty;

    /// <summary>
    /// 回显字段
    /// </summary>
    [JsonPropertyName("echo")]
    public string? Echo { get; set; }
}

/// <summary>
/// 响应体中 "data" 字段对应的结构
/// </summary>
public class MessageHistoryData
{
    /// <summary>
    /// 消息列表
    /// </summary>
    [JsonPropertyName("messages")]
    public List<GroupMessageEvent> Messages { get; set; } = new();
}