using System.Formats.Asn1;
using System.Linq.Expressions;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using qqbot.Models.Notifications;
using qqbot.RedisCache;

namespace qqbot.Helper;

/// <summary>
/// 一个专门用于日志记录的通知处理器。
/// 它同时订阅了私聊消息和群聊消息两种通知。
/// </summary>
public class LoggingHandler :
    INotificationHandler<PrivateMessageReceivedNotification>,
    INotificationHandler<GroupMessageReceivedNotification>
{
    private readonly ILogger<LoggingHandler> _logger;
    private readonly RedisService _redis;

    public LoggingHandler(ILogger<LoggingHandler> logger, RedisService redis)
    {
        _logger = logger;
        _redis = redis;
    }

    /// <summary>
    /// 处理“私聊消息”通知的方法
    /// </summary>
    public async Task Handle(PrivateMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        // 使用我们之前创建的 MessagePrint 工具来格式化消息
        string formattedMessage = MessagePrint.Format(notification.MessageEvent);

        // 使用标准的 ILogger 来记录结构化日志
        _logger.LogInformation("接收到私聊消息事件:\n{FormattedMessage}", formattedMessage);

        try
        {
            await _redis.PushPrivateMessageAsync("PrivateMessageQueue", notification.MessageEvent);
            _logger.LogInformation("已将消息推送到 Redis 队列 'PrivateMessageQueue'");  
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "将消息推送到 Redis 队列时出错");
        }
        
        return; //Task.CompletedTask;
    }

    /// <summary>
    /// 处理“群聊消息”通知的方法
    /// </summary>
    public async Task Handle(GroupMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        string formattedMessage = MessagePrint.Format(notification.MessageEvent);
        _logger.LogInformation("接收到群聊消息事件:\n{FormattedMessage}", formattedMessage);
        try
        {
            await _redis.PushGroupMessageAsync("GroupMessageQueue", notification.MessageEvent);
            _logger.LogInformation("已将消息推送到 Redis 队列 'GroupMessageQueue'"); 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "将消息推送到 Redis 队列时出错");
        }

        return; //Task.CompletedTask;
    }
}
