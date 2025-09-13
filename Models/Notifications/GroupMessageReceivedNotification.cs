using MediatR;
using static qqbot.Models.Group;

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