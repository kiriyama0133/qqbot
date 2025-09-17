using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using MediatR;
using qqbot.Models.Notifications;
using qqbot.Services;
using qqbot.Helper;

namespace qqbot.Handlers;

public class DataArchivingHandler :
    INotificationHandler<GroupMessageReceivedNotification>,
    INotificationHandler<PrivateMessageReceivedNotification>
{
    private readonly InfluxDbService _influxService;

    public DataArchivingHandler(InfluxDbService influxService)
    {
        _influxService = influxService;
    }

    public async Task Handle(GroupMessageReceivedNotification notification,  CancellationToken cancellationToken)
    {
        var message = notification.MessageEvent;
        
        // 使用 MessagePrint 格式化消息内容
        var formattedMessage = MessagePrint.FormatMessageSegments(message.Message);
        
        var point = PointData.Measurement("group_messages")
            .Tag("group_id", message.GroupId.ToString())
            .Tag("user_id", message.UserId.ToString())
            .Tag("sender_role", message.Sender?.Role ?? "unknown")
            .Tag("nickname", message.Sender?.Nickname ?? "未知")

            .Field("raw_message", message.RawMessage) // 原始消息
            .Field("formatted_message", formattedMessage) // 格式化后的消息
            .Field("message_length", message.RawMessage.Length) // 消息长度
            .Field("formatted_length", formattedMessage.Length) // 格式化后消息长度
            .Field("is_at_message", message.Message.Any(m => m.Type == "at"))
            .Field("is_file_message", message.Message.Any(m => m.Type == "file"))
            .Field("is_forward_message", message.Message.Any(m => m.Type == "forward"))
            .Field("is_image_message", message.Message.Any(m => m.Type == "image"))
            .Field("message_segment_count", message.Message.Count) // 消息段数量
            .Field("time", message.Time) // 发送时间

            .Timestamp(DateTime.UtcNow, WritePrecision.Ns);  // 使用 UTC 时间，精确到纳秒

        await _influxService.WritePointAsync(point);
    }

    public async Task Handle(PrivateMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var message = notification.MessageEvent;
        
        // 使用 MessagePrint 格式化消息内容
        var formattedMessage = MessagePrint.FormatMessageSegments(message.Message);
        
        var point = PointData.Measurement("private_message")
            .Tag("private_id", message.UserId.ToString())
            .Tag("nickname", message.Sender?.Nickname ?? "未知")

            .Field("raw_message", message.RawMessage) // 原始消息
            .Field("formatted_message", formattedMessage) // 格式化后的消息
            .Field("message_length", message.RawMessage.Length) // 消息长度
            .Field("formatted_length", formattedMessage.Length) // 格式化后消息长度
            .Field("is_file_message", message.Message.Any(m => m.Type == "file"))
            .Field("is_forward_message", message.Message.Any(m => m.Type == "forward"))
            .Field("is_image_message", message.Message.Any(m => m.Type == "image"))
            .Field("message_segment_count", message.Message.Count) // 消息段数量
            .Field("time", message.Time) // 发送时间

            .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

        await _influxService.WritePointAsync(point);
    }
}
