using qqbot.Abstractions;
using qqbot.Core.Services;
namespace qqbot.Helper;

/// <summary>
/// 一个后台托管服务，负责在应用启动时发现所有命令，
/// 并将它们注册到全局的 IDynamicStateService 中。
/// </summary>
public class CommandRegistry : IHostedService
{
    private readonly ILogger<CommandRegistry> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDynamicStateService _stateService;

    public CommandRegistry(
        ILogger<CommandRegistry> logger,
        IServiceProvider serviceProvider,
        IDynamicStateService stateService) // 注入全局状态服务
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _stateService = stateService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始从 DI 容器中发现并注册命令到全局状态...");

        // 为了避免在构造函数中进行重量级操作，我们在 StartAsync 中创建作用域
        using (var scope = _serviceProvider.CreateScope())
        {
            var commandHandlers = scope.ServiceProvider.GetServices<ICommandHandler>();
            var tempMap = new Dictionary<string, CommandDefinition>();

            foreach (var handler in commandHandlers)
            {
                var cmdDef = handler.Command;
                if (cmdDef == null || string.IsNullOrEmpty(cmdDef.Name)) continue;

                // ... (之前的命令冲突检查逻辑保持不变) ...
                if (!tempMap.TryAdd(cmdDef.Name, cmdDef))
                {
                    _logger.LogWarning("命令冲突: 命令 '{Command}' 已被注册。", cmdDef.Name);
                }
            }

            // 设置到全局状态服务中
            _stateService.SetState(StateKeys.Commands, tempMap);

            _logger.LogInformation("✅ [CommandRegistry] 成功将 {Count} 个主命令注册到全局状态。", tempMap.Count);
        }

        return Task.CompletedTask;
    }

    // StopAsync 对于这个一次性任务来说，无需实现
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// 定义所有全局状态的键
/// </summary>
public static class StateKeys
{
    /// <summary>
    /// 存储所有已注册命令的字典
    /// </summary>
    public const string Commands = "Commands.Map";
}