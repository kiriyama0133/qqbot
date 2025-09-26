using MediatR;
using Microsoft.EntityFrameworkCore;
using qqbot.Data;
using qqbot.Models.Entities;
using qqbot.Models.Events;
using qqbot.Models.Messages;
using qqbot.Models.Notifications;
using System.Text.Json;

namespace qqbot.Handlers;

/// <summary>
/// 聊天消息存储处理器 - 将聊天记录保存到 PostgreSQL
/// </summary>
public class ChatMessageStorageHandler :
    INotificationHandler<GroupMessageReceivedNotification>,
    INotificationHandler<PrivateMessageReceivedNotification>
{
    private readonly ILogger<ChatMessageStorageHandler> _logger;
    private readonly IDbContextFactory<MessageDbContext> _dbContextFactory;

    public ChatMessageStorageHandler(
        ILogger<ChatMessageStorageHandler> logger,
        IDbContextFactory<MessageDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// 处理群消息并保存到数据库
    /// </summary>
    public async Task Handle(GroupMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var messageEvent = notification.MessageEvent;
            var chatMessage = ConvertToChatMessage(messageEvent);
            
            await SaveChatMessageAsync(chatMessage, cancellationToken);
            
            _logger.LogDebug("群消息已保存到数据库: 群ID={GroupId}, 消息ID={MessageId}", 
                messageEvent.GroupId, messageEvent.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存群消息到数据库时发生错误: 群ID={GroupId}, 消息ID={MessageId}",
                notification.MessageEvent.GroupId, notification.MessageEvent.MessageId);
        }
    }

    /// <summary>
    /// 处理私聊消息并保存到数据库
    /// </summary>
    public async Task Handle(PrivateMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var messageEvent = notification.MessageEvent;
            var chatMessage = ConvertToChatMessage(messageEvent);
            
            await SaveChatMessageAsync(chatMessage, cancellationToken);
            
            _logger.LogDebug("私聊消息已保存到数据库: 用户ID={UserId}, 消息ID={MessageId}",
                messageEvent.UserId, messageEvent.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存私聊消息到数据库时发生错误: 用户ID={UserId}, 消息ID={MessageId}",
                notification.MessageEvent.UserId, notification.MessageEvent.MessageId);
        }
    }

    /// <summary>
    /// 将群消息事件转换为 ChatMessage 实体
    /// </summary>
    private ChatMessage ConvertToChatMessage(GroupMessageEvent messageEvent)
    {
        var messageSegments = messageEvent.Message ?? new List<MessageSegment>();
        var formattedMessage = MessagePrint.FormatMessageSegments(messageSegments);
        var rawMessageJson = SerializeMessageSegments(messageSegments);
        
        var metadata = AnalyzeMessageSegments(messageSegments);

        return new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            ChatType = "group",
            MessageType = messageEvent.MessageType ?? "group",
            SubType = messageEvent.SubType ?? string.Empty,
            MessageId = messageEvent.MessageId,
            MessageSequence = null, // 群消息没有序列号
            UserId = messageEvent.UserId,
            SenderNickname = messageEvent.Sender?.Nickname ?? string.Empty,
            SenderCard = messageEvent.Sender?.Card ?? string.Empty,
            SenderRole = messageEvent.Sender?.Role ?? string.Empty,
            GroupId = messageEvent.GroupId,
            PrivateUserId = 0, // 群消息时为空
            SelfId = messageEvent.SelfId,
            RawMessage = TruncateString(rawMessageJson, 10000),
            FormattedMessage = TruncateString(formattedMessage, 5000),
            MessageSegmentCount = messageSegments.Count,
            HasAtMessage = metadata.HasAtMessage,
            HasImage = metadata.HasImage,
            HasFile = metadata.HasFile,
            HasForward = metadata.HasForward,
            Font = messageEvent.Font,
            Time = messageEvent.Time,
            CreatedAt = DateTime.UtcNow,
            IsRecalled = false
        };
    }

    /// <summary>
    /// 将私聊消息事件转换为 ChatMessage 实体
    /// </summary>
    private ChatMessage ConvertToChatMessage(PrivateMessageEvent messageEvent)
    {
        var messageSegments = messageEvent.Message ?? new List<MessageSegment>();
        var formattedMessage = MessagePrint.FormatMessageSegments(messageSegments);
        var rawMessageJson = SerializeMessageSegments(messageSegments);
        
        var metadata = AnalyzeMessageSegments(messageSegments);

        return new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            ChatType = "private",
            MessageType = messageEvent.MessageType ?? "private",
            SubType = messageEvent.SubType ?? string.Empty,
            MessageId = messageEvent.MessageId,
            MessageSequence = messageEvent.MessageSequence,
            UserId = messageEvent.UserId,
            SenderNickname = messageEvent.Sender?.Nickname ?? string.Empty,
            SenderCard = string.Empty, // 私聊没有群名片
            SenderRole = string.Empty, // 私聊没有角色
            GroupId = 0, // 私聊时为空
            PrivateUserId = messageEvent.UserId,
            SelfId = messageEvent.SelfId,
            RawMessage = TruncateString(rawMessageJson, 10000),
            FormattedMessage = TruncateString(formattedMessage, 5000),
            MessageSegmentCount = messageSegments.Count,
            HasAtMessage = false, // 私聊没有 @ 消息
            HasImage = metadata.HasImage,
            HasFile = metadata.HasFile,
            HasForward = metadata.HasForward,
            Font = messageEvent.Font,
            Time = messageEvent.Time,
            CreatedAt = DateTime.UtcNow,
            IsRecalled = false
        };
    }

    /// <summary>
    /// 分析消息段，提取元数据
    /// </summary>
    private (bool HasAtMessage, bool HasImage, bool HasFile, bool HasForward) AnalyzeMessageSegments(
        List<MessageSegment> segments)
    {
        bool hasAtMessage = false;
        bool hasImage = false;
        bool hasFile = false;
        bool hasForward = false;

        foreach (var segment in segments)
        {
            switch (segment)
            {
                case AtMessageSegment:
                    hasAtMessage = true;
                    break;
                case ImageMessageSegment:
                    hasImage = true;
                    break;
                case FileMessageSegment:
                    hasFile = true;
                    break;
                case ForwardMessageSegment:
                    hasForward = true;
                    break;
            }
        }

        return (hasAtMessage, hasImage, hasFile, hasForward);
    }

    /// <summary>
    /// 将消息段序列化为 JSON 字符串
    /// </summary>
    private string SerializeMessageSegments(List<MessageSegment> segments)
    {
        try
        {
            return JsonSerializer.Serialize(segments, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "序列化消息段时发生错误");
            return "[]";
        }
    }

    /// <summary>
    /// 截断字符串到指定长度
    /// </summary>
    private string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        
        if (value.Length <= maxLength)
            return value;
        
        return value.Substring(0, maxLength);
    }

    /// <summary>
    /// 保存聊天消息到数据库
    /// </summary>
    private async Task SaveChatMessageAsync(ChatMessage chatMessage, CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        try
        {
            context.ChatMessages.Add(chatMessage);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "数据库更新失败: 消息ID={MessageId}", chatMessage.MessageId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存聊天消息时发生未知错误: 消息ID={MessageId}", chatMessage.MessageId);
            throw;
        }
    }
}

