using MediatR;
using qqbot.Models.Events;

namespace qqbot.Models.Notifications;

/// <summary>
/// 当收到群消息时，将发布的通知
/// </summary>
public class GroupMessageReceivedNotification : INotification
{
    public GroupMessageEvent MessageEvent { get; }

    public GroupMessageReceivedNotification(GroupMessageEvent messageEvent)
    {
        MessageEvent = messageEvent;
    }
}