using MediatR;

namespace qqbot.Models.Notifications;

/// <summary>
    /// 当收到私聊消息时，将发布的通知
    /// </summary>
    public class PrivateMessageReceivedNotification : INotification
{
    public PrivateMessageEvent MessageEvent { get; }

    public PrivateMessageReceivedNotification(PrivateMessageEvent messageEvent)
    {
        MessageEvent = messageEvent;
    }
}