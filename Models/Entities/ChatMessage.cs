using System.ComponentModel.DataAnnotations;

namespace qqbot.Models.Entities;

/// <summary>
/// 聊天消息记录实体，用于存储到 PostgreSQL
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// 主键 ID (Guid 字符串格式)
    /// </summary>
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 聊天类型：group (群聊) 或 private (私聊)
    /// </summary>
    [MaxLength(10)]
    public required string ChatType { get; set; }

    /// <summary>
    /// 消息类型：text, image, at, face, reply, file, forward 等
    /// </summary>
    [MaxLength(20)]
    public required string MessageType { get; set; }

    /// <summary>
    /// 子类型（如 friend, group, normal 等）
    /// </summary>
    [MaxLength(20)]
    public string SubType { get; set; } = string.Empty;

    /// <summary>
    /// QQ 消息 ID（来自 NapCat/OneBot）
    /// </summary>
    public long MessageId { get; set; }

    /// <summary>
    /// 消息序列号（私聊消息使用）
    /// </summary>
    public long? MessageSequence { get; set; }

    /// <summary>
    /// 发送者用户 ID
    /// </summary>
    public required long UserId { get; set; }

    /// <summary>
    /// 发送者昵称
    /// </summary>
    [MaxLength(100)]
    public string SenderNickname { get; set; } = string.Empty;

    /// <summary>
    /// 发送者群名片（群聊时使用）
    /// </summary>
    [MaxLength(100)]
    public string SenderCard { get; set; } = string.Empty;

    /// <summary>
    /// 发送者角色（群主、管理员、成员等）
    /// </summary>
    [MaxLength(20)]
    public string SenderRole { get; set; } = string.Empty;

    /// <summary>
    /// 群 ID（群聊时使用，私聊时为 0）
    /// </summary>
    public long GroupId { get; set; }

    /// <summary>
    /// 私聊对象 ID（私聊时使用，群聊时为 0）
    /// </summary>
    public long PrivateUserId { get; set; }

    /// <summary>
    /// 机器人自身 ID
    /// </summary>
    public required long SelfId { get; set; }

    /// <summary>
    /// 原始消息内容（JSON 格式的消息段数组）
    /// </summary>
    [MaxLength(10000)]
    public string RawMessage { get; set; } = string.Empty;

    /// <summary>
    /// 格式化后的消息内容（纯文本形式）
    /// </summary>
    [MaxLength(5000)]
    public string FormattedMessage { get; set; } = string.Empty;

    /// <summary>
    /// 消息段数量
    /// </summary>
    public int MessageSegmentCount { get; set; }

    /// <summary>
    /// 是否包含 @ 消息
    /// </summary>
    public bool HasAtMessage { get; set; }

    /// <summary>
    /// 是否包含图片
    /// </summary>
    public bool HasImage { get; set; }

    /// <summary>
    /// 是否包含文件
    /// </summary>
    public bool HasFile { get; set; }

    /// <summary>
    /// 是否包含转发消息
    /// </summary>
    public bool HasForward { get; set; }

    /// <summary>
    /// 字体 ID
    /// </summary>
    public int Font { get; set; }

    /// <summary>
    /// 消息时间戳（Unix 时间戳，秒）
    /// </summary>
    public long Time { get; set; }

    /// <summary>
    /// 创建时间（数据库记录创建时间）
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间（数据库记录更新时间）
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 是否已撤回
    /// </summary>
    public bool IsRecalled { get; set; }

    /// <summary>
    /// 撤回时间
    /// </summary>
    public DateTime? RecalledAt { get; set; }
}

