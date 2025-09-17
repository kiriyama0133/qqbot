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
[JsonDerivedType(typeof(FileMessageSegment), "file")]
[JsonDerivedType(typeof(ForwardMessageSegment), "forward")]
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
    public static FileMessageSegment File(string file, string fileId, string fileSize) => new() { 
        Data = new FileData { 
            File = file, 
            FileId = fileId, 
            FileSize = fileSize 
        } 
    };
    public static ForwardMessageSegment Forward(string id) => new() { 
        Data = new ForwardData { 
            Id = id 
        } 
    };

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

/// <summary>
/// 文件消息段
/// </summary>
public class FileMessageSegment : MessageSegment
{
    public FileMessageSegment() { Type = "file"; }
    [JsonPropertyName("data")]
    public FileData? Data { get; set; }
}
public class FileData
{
    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;
    
    [JsonPropertyName("file_id")]
    public string FileId { get; set; } = string.Empty;
    
    [JsonPropertyName("file_size")]
    public string FileSize { get; set; } = string.Empty;
    
    /// <summary>
    /// 获取文件的下载URL（基于file_id）
    /// </summary>
    public string GetDownloadUrl()
    {
        if (string.IsNullOrEmpty(FileId))
            return string.Empty;
            
        // QQ文件下载URL格式（需要根据实际API调整）
        return $"https://grouptalk.c2c.qq.com/download?file_id={FileId}";
    }
    
    /// <summary>
    /// 获取文件大小的人类可读格式
    /// </summary>
    public string GetFormattedFileSize()
    {
        if (string.IsNullOrEmpty(FileSize) || !long.TryParse(FileSize, out long size))
            return "未知大小";
            
        if (size < 1024)
            return $"{size} B";
        else if (size < 1024 * 1024)
            return $"{size / 1024.0:F1} KB";
        else if (size < 1024 * 1024 * 1024)
            return $"{size / (1024.0 * 1024.0):F1} MB";
        else
            return $"{size / (1024.0 * 1024.0 * 1024.0):F1} GB";
    }
}

/// <summary>
/// 转发消息段（聊天记录）
/// </summary>
public class ForwardMessageSegment : MessageSegment
{
    public ForwardMessageSegment() { Type = "forward"; }
    [JsonPropertyName("data")]
    public ForwardData? Data { get; set; }
}
public class ForwardData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// 获取转发消息的查看URL
    /// </summary>
    public string GetViewUrl()
    {
        if (string.IsNullOrEmpty(Id))
            return string.Empty;
            
        // QQ转发消息查看URL格式（需要根据实际API调整）
        return $"文件加载等待开发{Id}";
    }
}

public class TextMessageSegmentData : MessageSegment
{
    [JsonPropertyName("data")]
    public TextData? Data { get; set; }
}