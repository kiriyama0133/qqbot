using System.Text.Json.Serialization;

namespace qqbot.Models.Messages;

/// <summary>
/// 消息段的基类。
/// 使用 JsonPolymorphic 特性来自动反序列化为正确的子类型。
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

    public static TextMessageSegment Text(string text) => new() { Data = new TextData { Text = text } };
    public static ImageMessageSegment Image(string file, string url = "") => new() { 
        Data = new ImageData {
            File = file,
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
}

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
    
    public string GetDownloadUrl()
    {
        if (string.IsNullOrEmpty(FileId))
            return string.Empty;
        return $"未定义,文件api等待补充{FileId}";
    }
    
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
    
    public string GetViewUrl()
    {
        if (string.IsNullOrEmpty(Id))
            return string.Empty;
        return $"文件加载等待开发{Id}";
    }
}
