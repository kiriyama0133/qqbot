using MediatR;
using qqbot.Models.Notifications;

namespace qqbot.Handlers;

/// <summary>
/// 日志处理器 - 记录私聊和群聊消息
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

    public Task Handle(PrivateMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        string formattedMessage = MessagePrint.Format(notification.MessageEvent);
        _logger.LogInformation("接收到私聊消息事件:\n{FormattedMessage}", formattedMessage);
        return Task.CompletedTask;
    }

    public Task Handle(GroupMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        string formattedMessage = MessagePrint.Format(notification.MessageEvent);
        _logger.LogInformation("接收到群聊消息事件:\n{FormattedMessage}", formattedMessage);
        return Task.CompletedTask;
    }
}
