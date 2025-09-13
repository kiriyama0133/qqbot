using MediatR;
using qqbot.Abstractions;
using qqbot.Models;
using qqbot.Models.Notifications;
using qqbot.Services;

namespace qqbot.Handlers;


/// <summary>
/// 这个处理器专门订阅“群消息收到”的通知
/// </summary>
public class PingPongHandler : 
    INotificationHandler<GroupMessageReceivedNotification>,
    INotificationHandler<PrivateMessageReceivedNotification>,
    ICommandHandler

{
    private readonly ILogger<PingPongHandler> _logger;
    private readonly NapCatApiService _napCatApiService;
    public CommandDefinition Command { get; } = new()
    {
        Name = "/ping",
        Description = "测试机器人连通性与延迟。",
        Aliases = new List<string> { "/test" },
        SubCommands = new List<CommandDefinition>
            {
                // 👇 在这里定义一个子命令
                new CommandDefinition
                {
                    Name = "help",
                    Description = "显示 ping 命令的详细用法。",
                    Arguments = new List<CommandArgument>
                    {
                        new CommandArgument
                        {
                            Name = "test",
                            Description = "参数的描述测试",
                            IsRequired = false
                        }
                    }
                }
            }
    };

    // 通过构造函数注入它所依赖的服务
    public PingPongHandler(ILogger<PingPongHandler> logger, NapCatApiService napCatApiService)
    {
        _logger = logger;
        _napCatApiService = napCatApiService;
    }

    /// <summary>
    /// 这是 MediatR 规定必须实现的 Handle 方法
    /// 当有 GroupMessageReceivedNotification 发布时，这个方法会被自动调用
    /// </summary>
    public async Task Handle(GroupMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        // 从通知中获取原始的群消息事件
        var messageEvent = notification.MessageEvent;

        // --- 在这里编写您的核心业务逻辑 ---
        if (messageEvent.RawMessage.Trim().Equals("/ping", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("PingPongHandler 已触发！准备向群 {GroupId} 回复 'pong!'...", messageEvent.GroupId);

            // 构建回复消息
            var replyMessage = new List<MessageSegment>
                {
                    MessageSegment.Text("pong!")
                };

            // 调用 NapCatApiService 来发送消息
            await _napCatApiService.SendGroupMessageAsync(messageEvent.GroupId, replyMessage);
        }
    }


    /// <summary>
    /// 处理“私聊消息”通知的方法
    /// </summary>
    public async Task Handle(PrivateMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var messageEvent = notification.MessageEvent;
        if (messageEvent.RawMessage.Trim().Equals("/ping", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("PingPongHandler 触发 (来自私聊 {UserId})！准备回复 'pong!'...", messageEvent.UserId);

            var replyMessage = new List<MessageSegment> { MessageSegment.Text("pong!") };
            await _napCatApiService.SendPrivateMessageAsync(messageEvent.UserId, replyMessage);
        }
    }


}
