using System.Text.Json.Serialization;

namespace qqbot.Models;

public class SharedMessageModels
{

    // ---------------------------------------------
    // --- 所有可重用的模型都集中定义在这里 ---
    // ---------------------------------------------

    /// <summary>
    /// 代表发送者的信息
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

    /// <summary>
    /// 消息段的基类。
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(TextMessageSegment), "text")]
    [JsonDerivedType(typeof(ImageMessageSegment), "image")]
    [JsonDerivedType(typeof(AtMessageSegment), "at")]
    [JsonDerivedType(typeof(FaceMessageSegment), "face")]
    [JsonDerivedType(typeof(ReplyMessageSegment), "reply")]
    [JsonDerivedType(typeof(FileMessageSegment), "file")]
    public class MessageSegment
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        #region --- 消息段工厂方法 ---
        public static TextMessageSegment Text(string text) => new() { Data = new TextData { Text = text } };
        public static ImageMessageSegment Image(string file) => new() { Data = new ImageData { File = file } };
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
        #endregion
    }

    #region --- 消息段具体实现类 ---

    public class TextMessageSegment : MessageSegment
    {
        public TextMessageSegment() { Type = "text"; }
        [JsonPropertyName("data")]
        public TextData? Data { get; set; }
    }
    public class TextData { [JsonPropertyName("text")] public string Text { get; set; } = string.Empty; }

    public class ImageMessageSegment : MessageSegment
    {
        public ImageMessageSegment() { Type = "image"; }
        [JsonPropertyName("data")]
        public ImageData? Data { get; set; }
    }
    public class ImageData { [JsonPropertyName("file")] public string File { get; set; } = string.Empty; [JsonPropertyName("url")] public string Url { get; set; } = string.Empty; }

    public class AtMessageSegment : MessageSegment
    {
        public AtMessageSegment() { Type = "at"; }
        [JsonPropertyName("data")]
        public AtData? Data { get; set; }
    }
    public class AtData { [JsonPropertyName("qq")] public string Qq { get; set; } = string.Empty; }

    public class FaceMessageSegment : MessageSegment
    {
        public FaceMessageSegment() { Type = "face"; }
        [JsonPropertyName("data")]
        public FaceData? Data { get; set; }
    }
    public class FaceData { [JsonPropertyName("id")] public string Id { get; set; } = string.Empty; }

    public class ReplyMessageSegment : MessageSegment
    {
        public ReplyMessageSegment() { Type = "reply"; }
        [JsonPropertyName("data")]
        public ReplyData? Data { get; set; }
    }
    public class ReplyData { [JsonPropertyName("id")] public string MessageId { get; set; } = string.Empty; }

    public class FileMessageSegment : MessageSegment
    {
        public FileMessageSegment() { Type = "file"; }
        [JsonPropertyName("data")]
        public FileData? Data { get; set; }
    }
    public class FileData 
    { 
        [JsonPropertyName("file")] public string File { get; set; } = string.Empty; 
        [JsonPropertyName("file_id")] public string FileId { get; set; } = string.Empty; 
        [JsonPropertyName("file_size")] public string FileSize { get; set; } = string.Empty; 
        
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

    #endregion

}
