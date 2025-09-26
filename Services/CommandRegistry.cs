using qqbot.Abstractions;
using qqbot.Core.Services;

namespace qqbot.Services;

/// <summary>
/// 命令注册服务 - 在应用启动时发现并注册所有命令到全局状态
/// </summary>
public class CommandRegistry : IHostedService
{
    private readonly ILogger<CommandRegistry> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDynamicStateService _stateService;

    public CommandRegistry(
        ILogger<CommandRegistry> logger,
        IServiceProvider serviceProvider,
        IDynamicStateService stateService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _stateService = stateService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始发现并注册命令到全局状态...");

        using (var scope = _serviceProvider.CreateScope())
        {
            var commandHandlers = scope.ServiceProvider.GetServices<ICommandHandler>();
            var tempMap = new Dictionary<string, CommandDefinition>();

            foreach (var handler in commandHandlers)
            {
                var cmdDef = handler.Command;
                if (cmdDef == null || string.IsNullOrEmpty(cmdDef.Name)) continue;

                if (!tempMap.TryAdd(cmdDef.Name, cmdDef))
                {
                    _logger.LogWarning("命令冲突: 命令 '{Command}' 已被注册。", cmdDef.Name);
                }
            }

            _stateService.SetState(StateKeys.Commands, tempMap);
            _logger.LogInformation("✅ 成功将 {Count} 个主命令注册到全局状态。", tempMap.Count);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// 全局状态键定义
/// </summary>
public static class StateKeys
{
    /// <summary>
    /// 存储所有已注册命令的字典
    /// </summary>
    public const string Commands = "Commands.Map";
}
