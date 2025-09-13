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

    #endregion

}
