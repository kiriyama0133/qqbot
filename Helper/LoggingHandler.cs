using MediatR;
using qqbot.Models.Notifications;

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

    public LoggingHandler(ILogger<LoggingHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 处理“私聊消息”通知的方法
    /// </summary>
    public Task Handle(PrivateMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        // 使用我们之前创建的 MessagePrint 工具来格式化消息
        string formattedMessage = MessagePrint.Format(notification.MessageEvent);

        // 使用标准的 ILogger 来记录结构化日志
        _logger.LogInformation("接收到私聊消息事件:\n{FormattedMessage}", formattedMessage);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 处理“群聊消息”通知的方法
    /// </summary>
    public Task Handle(GroupMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        string formattedMessage = MessagePrint.Format(notification.MessageEvent);
        _logger.LogInformation("接收到群聊消息事件:\n{FormattedMessage}", formattedMessage);

        return Task.CompletedTask;
    }
}
