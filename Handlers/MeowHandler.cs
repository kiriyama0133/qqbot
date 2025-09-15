using MediatR;
using qqbot.Abstractions;
using qqbot.Models;
using qqbot.Models.Notifications;
using qqbot.Services;

namespace qqbot.Handlers;

/// <summary>
/// Meow命令处理器，发送可爱的猫咪图片
/// </summary>
public class MeowHandler : 
    INotificationHandler<GroupMessageReceivedNotification>,
    INotificationHandler<PrivateMessageReceivedNotification>,
    ICommandHandler
{
    private readonly ILogger<MeowHandler> _logger;
    private readonly NapCatApiService _napCatApiService;

    public CommandDefinition Command { get; } = new()
    {
        Name = "/meow",
        Description = "发送可爱的猫咪图片(迫真)",
        Aliases = new List<string> { "/喵", "/猫猫", "/猫咪" }
    };

    public MeowHandler(
        ILogger<MeowHandler> logger,
        NapCatApiService napCatApiService)
    {
        _logger = logger;
        _napCatApiService = napCatApiService;
    }

    public async Task Handle(GroupMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var messageEvent = notification.MessageEvent;
        if (IsMeowCommand(messageEvent.RawMessage))
        {
            await SendMeowImage(messageEvent.GroupId, isPrivate: false);
        }
    }

    public async Task Handle(PrivateMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var messageEvent = notification.MessageEvent;
        if (IsMeowCommand(messageEvent.RawMessage))
        {
            await SendMeowImage(messageEvent.UserId, isPrivate: true);
        }
    }

    private bool IsMeowCommand(string message)
    {
        var commandText = message.Trim();
        return commandText.Equals(Command.Name, StringComparison.OrdinalIgnoreCase) ||
               Command.Aliases.Any(alias => commandText.Equals(alias, StringComparison.OrdinalIgnoreCase));
    }

    private async Task SendMeowImage(long targetId, bool isPrivate)
    {
        try
        {
            // 获取图片文件路径
            var imagePath = GetMeowImagePath();
            
            if (!File.Exists(imagePath))
            {
                _logger.LogWarning("Meow图片文件不存在: {ImagePath}", imagePath);
                await SendErrorMessage(targetId, isPrivate, "抱歉，猫咪图片暂时不可用~");
                return;
            }

            // 创建图片消息
            var imageMessage = new List<MessageSegment> 
            { 
                MessageSegment.Image(imagePath, "") // 使用本地文件路径
            };

            // 发送消息
            if (isPrivate)
            {
                await _napCatApiService.SendPrivateMessageAsync(targetId, imageMessage);
                _logger.LogInformation("已向用户 {UserId} 发送Meow图片", targetId);
            }
            else
            {
                await _napCatApiService.SendGroupMessageAsync(targetId, imageMessage);
                _logger.LogInformation("已向群 {GroupId} 发送Meow图片", targetId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送Meow图片时发生错误");
            await SendErrorMessage(targetId, isPrivate, "发送图片时出错了，请稍后再试~");
        }
    }

    private async Task SendErrorMessage(long targetId, bool isPrivate, string errorMessage)
    {
        try
        {
            var textMessage = new List<MessageSegment> { MessageSegment.Text(errorMessage) };
            
            if (isPrivate)
            {
                await _napCatApiService.SendPrivateMessageAsync(targetId, textMessage);
            }
            else
            {
                await _napCatApiService.SendGroupMessageAsync(targetId, textMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送错误消息时发生错误");
        }
    }

    private string GetMeowImagePath()
    {
        // 尝试多个可能的图片路径
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory,"Publics", "defalut.jpg"),
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                _logger.LogDebug("找到Meow图片: {ImagePath}", path);
                return path;
            }
        }

        // 如果都没找到，返回第一个路径（用于错误处理）
        _logger.LogWarning("未找到Meow图片文件，尝试的路径: {Paths}", string.Join(", ", possiblePaths));
        return possiblePaths[0];
    }
}
