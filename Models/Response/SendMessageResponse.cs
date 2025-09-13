using System.Text.Json.Serialization;
using static qqbot.Models.SharedMessageModels;

namespace qqbot.Models.Response;

public class SendMessageResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("retcode")]
    public int ReturnCode { get; set; }

    [JsonPropertyName("data")]
    public ResponseData? Data { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("echo")]
    public string? Echo { get; set; }

    /// <summary>
    /// 代表 /send_... API 响应中 "data" 字段的结构。
    /// 这是一个嵌套类，专属于 SendMessageResponse。
    /// </summary>
    public class ResponseData
    {
        [JsonPropertyName("message_id")]
        public long MessageId { get; set; }
    }

}
