using MediatR;
using qqbot.Abstractions;
using qqbot.Core.Services;
using qqbot.Models.Messages;
using qqbot.Models.Notifications;
using qqbot.Services;
using qqbot.Services.Plugins;
using System.Diagnostics;
using System.Text.Json;

namespace qqbot.Handlers;

/// <summary>
/// 状态监控控制命令处理器
/// </summary>
public class StateMonitorHandler : 
    INotificationHandler<GroupMessageReceivedNotification>,
    INotificationHandler<PrivateMessageReceivedNotification>,
    ICommandHandler
{
    private readonly ILogger<StateMonitorHandler> _logger;
    private readonly StateMonitorService _stateMonitorService;
    private readonly IDynamicStateService _stateService;
    private readonly NapCatApiService _napCatApiService;

    public CommandDefinition Command { get; } = new()
    {
        Name = "/monitor",
        Description = "控制全局状态监控服务",
        Aliases = new List<string> { "/状态监控", "/监控" },
        SubCommands = new List<CommandDefinition>
        {
            new CommandDefinition
            {
                Name = "status",
                Description = "查看监控状态",
                Aliases = new List<string> { "状态" }
            },
            new CommandDefinition
            {
                Name = "enable",
                Description = "启用监控",
                Aliases = new List<string> { "开启", "on" }
            },
            new CommandDefinition
            {
                Name = "disable",
                Description = "禁用监控",
                Aliases = new List<string> { "关闭", "off" }
            },
            new CommandDefinition
            {
                Name = "interval",
                Description = "设置监控间隔（秒）",
                Aliases = new List<string> { "间隔", "time" },
                Arguments = new List<CommandArgument>
                {
                    new CommandArgument
                    {
                        Name = "seconds",
                        Description = "监控间隔秒数（最小1秒）",
                        IsRequired = true
                    }
                }
            },
            new CommandDefinition
            {
                Name = "trigger",
                Description = "手动触发一次监控",
                Aliases = new List<string> { "触发", "now" }
            },
            new CommandDefinition
            {
                Name = "config",
                Description = "查看监控配置",
                Aliases = new List<string> { "配置", "settings" }
            }
        }
    };

    public StateMonitorHandler(
        ILogger<StateMonitorHandler> logger,
        StateMonitorService stateMonitorService,
        IDynamicStateService stateService,
        NapCatApiService napCatApiService)
    {
        _logger = logger;
        _stateMonitorService = stateMonitorService;
        _stateService = stateService;
        _napCatApiService = napCatApiService;
    }

    public async Task Handle(GroupMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var messageEvent = notification.MessageEvent;
        if (IsMonitorCommand(messageEvent.RawMessage))
        {
            var (message, imagePath) = await ProcessMonitorCommand(messageEvent.RawMessage);
            var replyMessage = new List<MessageSegment> { MessageSegment.Text(message) };
            
            // 如果有图片路径，添加图片消息段
            if (!string.IsNullOrEmpty(imagePath))
            {
                replyMessage.Add(MessageSegment.Image(file: imagePath, url: ""));
            }
            
            await _napCatApiService.SendGroupMessageAsync(messageEvent.GroupId, replyMessage);
        }
    }

    public async Task Handle(PrivateMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var messageEvent = notification.MessageEvent;
        if (IsMonitorCommand(messageEvent.RawMessage))
        {
            var (message, imagePath) = await ProcessMonitorCommand(messageEvent.RawMessage);
            var replyMessage = new List<MessageSegment> { MessageSegment.Text(message) };
            
            // 如果有图片路径，添加图片消息段
            if (!string.IsNullOrEmpty(imagePath))
            {
                replyMessage.Add(MessageSegment.Image(file: imagePath, url: ""));
            }
            
            await _napCatApiService.SendPrivateMessageAsync(messageEvent.UserId, replyMessage);
        }
    }

    private bool IsMonitorCommand(string message)
    {
        var commandText = message.Trim();
        return commandText.StartsWith(Command.Name, StringComparison.OrdinalIgnoreCase) ||
               Command.Aliases.Any(alias => commandText.StartsWith(alias, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<(string Message, string? ImagePath)> ProcessMonitorCommand(string message)
    {
        try
        {
            var parts = message.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return ("用法: /monitor <子命令>\n可用子命令: status, enable, disable, interval, trigger, config", null);
            }

            var subCommand = parts[1].ToLower();
            return subCommand switch
            {
                "status" or "状态" => (GetMonitorStatus(), null),
                "enable" or "开启" or "on" => (EnableMonitor(), null),
                "disable" or "关闭" or "off" => (DisableMonitor(), null),
                "interval" or "间隔" or "time" => (SetMonitorInterval(parts), null),
                "trigger" or "触发" or "now" => (TriggerMonitor(), null),
                "config" or "配置" or "settings" => (GetMonitorConfig(), null),
                _ => ("未知子命令。可用命令: status, enable, disable, interval, trigger, config", null)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理监控命令时发生错误");
            return ($"处理命令时发生错误: {ex.Message}", null);
        }
    }

    private string GetMonitorStatus()
    {
        var config = _stateMonitorService.GetMonitorConfig();
        var status = _stateService.GetState<string>(StateMonitorKeys.MonitorStatus, "Unknown");
        var lastTime = _stateService.GetState<DateTime?>(StateMonitorKeys.LastMonitorTime);

        var statusText = $"状态监控状态\n" +
                        $"启用: {(config.IsEnabled ? "是" : "否")}\n" +
                        $"间隔: {config.IntervalSeconds}秒\n" +
                        $"状态: {status}\n" +
                        $"最后监控: {lastTime?.ToString("HH:mm:ss") ?? "从未"}";

        return statusText;
    }

    private string EnableMonitor()
    {
        _stateMonitorService.SetMonitorEnabled(true);
        return "状态监控已启用";
    }

    private string DisableMonitor()
    {
        _stateMonitorService.SetMonitorEnabled(false);
        return "状态监控已禁用";
    }

    private string SetMonitorInterval(string[] parts)
    {
        if (parts.Length < 3)
        {
            return "用法: /monitor interval <秒数>";
        }

        if (!int.TryParse(parts[2], out var seconds) || seconds < 1)
        {
            return "错误: 间隔必须是大于0的整数";
        }

        _stateMonitorService.SetMonitorInterval(seconds);
        return $"监控间隔已设置为 {seconds} 秒";
    }

    private string TriggerMonitor()
    {
        _stateMonitorService.TriggerMonitor();
        return "已手动触发状态监控";
    }

    private string GetMonitorConfig()
    {
        var config = _stateMonitorService.GetMonitorConfig();
        
        var configText = $"监控配置\n" +
                        $"启用: {(config.IsEnabled ? "是" : "否")}\n" +
                        $"间隔: {config.IntervalSeconds}秒\n" +
                        $"详细信息: {(config.ShowDetailedInfo ? "是" : "否")}\n" +
                        $"仅显示变化: {(config.OnlyShowChanges ? "是" : "否")}\n" +
                        $"最大显示数: {config.MaxDisplayCount}\n" +
                        $"监控键数: {config.MonitoredKeys.Count}\n" +
                        $"排除键数: {config.ExcludedKeys.Count}";

        return configText;
    }


}
