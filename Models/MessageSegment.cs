using System.Text.Json;
using System.Text.Json.Serialization;

namespace qqbot.Models;

// ---------------------------------------------
// --- 所有可重用的消息模型都集中定义在这里 ---
// ---------------------------------------------

/// <summary>
/// 消息段的基类。
/// 使用 JsonPolymorphic 特性 (需要 .NET 7+) 来自动反序列化为正确的子类型。
/// 它告诉序列化器，根据 JSON 中的 "type" 字段的值来决定要创建哪个子类的实例。
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextMessageSegment), "text")]
[JsonDerivedType(typeof(ImageMessageSegment), "image")]
[JsonDerivedType(typeof(AtMessageSegment), "at")]
[JsonDerivedType(typeof(FaceMessageSegment), "face")]
[JsonDerivedType(typeof(ReplyMessageSegment), "reply")]
public class MessageSegment
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    #region --- 消息段工厂方法 (为了方便地构造要发送的消息) ---

    public static TextMessageSegment Text(string text) => new() { Data = new TextData { Text = text } };
    public static ImageMessageSegment Image(string file,string url) => new() { 
        Data = new ImageData {
            File = file ,
            Url = url
        } 
    };
    public static AtMessageSegment At(string qq) => new() { Data = new AtData { Qq = qq } };
    public static FaceMessageSegment Face(int id) => new() { Data = new FaceData { Id = id.ToString() } };
    public static ReplyMessageSegment Reply(int messageId) => new() { Data = new ReplyData { MessageId = messageId.ToString() } };

    #endregion
}

// --- 为每种消息段类型创建一个具体的类 ---

/// <summary>
/// 文本消息段
/// </summary>
public class TextMessageSegment : MessageSegment
{
    public TextMessageSegment() { Type = "text"; }
    [JsonPropertyName("data")]
    public TextData? Data { get; set; }
}
public class TextData
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// 图片消息段
/// </summary>
public class ImageMessageSegment : MessageSegment
{
    public ImageMessageSegment() { Type = "image"; }
    [JsonPropertyName("data")]
    public ImageData? Data { get; set; }
}
public class ImageData
{
    [JsonPropertyName("file")] public string File { get; set; } = string.Empty;
    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
}

/// <summary>
/// @某人消息段
/// </summary>
public class AtMessageSegment : MessageSegment
{
    public AtMessageSegment() { Type = "at"; }
    [JsonPropertyName("data")]
    public AtData? Data { get; set; }
}
public class AtData
{
    [JsonPropertyName("qq")]
    public string Qq { get; set; } = string.Empty;
}

/// <summary>
/// QQ表情消息段
/// </summary>
public class FaceMessageSegment : MessageSegment
{
    public FaceMessageSegment() { Type = "face"; }
    [JsonPropertyName("data")]
    public FaceData? Data { get; set; }
}
public class FaceData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// 回复消息段
/// </summary>
public class ReplyMessageSegment : MessageSegment
{
    public ReplyMessageSegment() { Type = "reply"; }
    [JsonPropertyName("data")]
    public ReplyData? Data { get; set; }
}
public class ReplyData
{
    [JsonPropertyName("id")]
    public string MessageId { get; set; } = string.Empty;
}

public class TextMessageSegmentData : MessageSegment
{
    [JsonPropertyName("data")]
    public TextData? Data { get; set; }
}