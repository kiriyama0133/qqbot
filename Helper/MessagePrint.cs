using qqbot.Models;
using System.Text;
using static qqbot.Models.Group;

namespace qqbot.Helper;

/// <summary>
/// 一个静态辅助类，用于将消息事件格式化为可读的字符串。
/// </summary>
public static class MessagePrint
{
    /// <summary>
    /// 格式化私聊消息事件
    /// </summary>
    public static string Format(PrivateMessageEvent e)
    {
        if (e?.Sender == null) return "[无效的私聊消息]";

        var builder = new StringBuilder();
        var time = DateTimeOffset.FromUnixTimeSeconds(e.Time).ToLocalTime();

        builder.AppendLine("┌───────────────────────────────────");
        builder.AppendLine($"| 收到私聊消息 (Private Message)");
        builder.AppendLine($"| 时间: {time:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"| 来自: {e.Sender.Nickname} (QQ: {e.Sender.UserId})");
        builder.AppendLine("├─ 消息内容 ───────────────");
        builder.Append("|  ").AppendLine(FormatMessageSegments(e.Message));
        builder.AppendLine("└───────────────────────────────────");

        return builder.ToString();
    }

    /// <summary>
    /// 格式化群聊消息事件
    /// </summary>
    public static string Format(GroupMessageEvent e)
    {
        if (e?.Sender == null) return "[无效的群聊消息]";

        var builder = new StringBuilder();
        var time = DateTimeOffset.FromUnixTimeSeconds(e.Time).ToLocalTime();

        builder.AppendLine("┌───────────────────────────────────");
        builder.AppendLine($"| 收到群聊消息 (Group Message)");
        builder.AppendLine($"| 时间: {time:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"| 来自群: {e.GroupId}"); // 之后可以扩展为显示群名称
        builder.AppendLine($"| 发送者: {e.Sender.Card ?? e.Sender.Nickname} (QQ: {e.Sender.UserId})");
        builder.AppendLine("├─ 消息内容 ───────────────");
        builder.Append("|  ").AppendLine(FormatMessageSegments(e.Message));
        builder.AppendLine("└───────────────────────────────────");

        return builder.ToString();
    }

    /// <summary>
    /// 将结构化的消息段列表转换为单行可读字符串
    /// </summary>
    private static string FormatMessageSegments(List<MessageSegment> segments)
    {
        if (segments == null || segments.Count == 0) return "[空消息]";

        var builder = new StringBuilder();
        foreach (var segment in segments)
        {
            // 使用 C# 模式匹配来处理不同类型的消息段
            switch (segment)
            {
                case TextMessageSegment text: 
                    builder.Append(text.Data?.Text);
                    break;
                case ImageMessageSegment image:
                    builder.Append($"[图片: {image.Data?.File},{image.Data?.Url}]");
                    break;
                case AtMessageSegment at: 
                    builder.Append($"[@{at.Data?.Qq}]");
                    break;
                case FaceMessageSegment face: 
                    builder.Append($"[表情:{face.Data?.Id}]");
                    break;
                case FileMessageSegment file:
                    var downloadUrl = file.Data?.GetDownloadUrl() ?? "";
                    var formattedSize = file.Data?.GetFormattedFileSize() ?? "未知大小";
                    builder.Append($"[文件: {file.Data?.File} (大小: {formattedSize})");
                    if (!string.IsNullOrEmpty(downloadUrl))
                    {
                        builder.Append($" - 下载: {downloadUrl}");
                    }
                    builder.Append("]");
                    break;
                case ReplyMessageSegment reply:
                    builder.Append($"[回复: {reply.Data?.MessageId}]");
                    break;
                default:
                    builder.Append($"[{segment.Type}: 未知内容]");
                    break;
            }
        }
        return builder.ToString();
    }
}
