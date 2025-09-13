using qqbot.Abstractions;
namespace qqbot.Helper;

/// <summary>
/// 一个单例服务，负责在启动时发现并存储所有已注册的命令。
/// </summary>
public class CommandRegistry
{
    /// <summary>
    /// 暴露一个将命令主名称映射到其完整定义的字典
    /// </summary>
    public IReadOnlyDictionary<string, CommandDefinition> Commands { get; private set; }

    private readonly ILogger<CommandRegistry> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CommandRegistry(ILogger<CommandRegistry> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        Commands = new Dictionary<string, CommandDefinition>();
    }

    public void Initialize()
    {
        _logger.LogInformation("开始从 DI 容器中发现并注册命令...");
        var commandHandlers = _serviceProvider.GetServices<ICommandHandler>();

        var tempMap = new Dictionary<string, CommandDefinition>();
        foreach (var handler in commandHandlers)
        {
            var cmdDef = handler.Command;
            if (cmdDef == null || string.IsNullOrEmpty(cmdDef.Name)) continue;

            // 只将主命令名作为 Key，确保每个命令定义只有一个入口
            if (!tempMap.TryAdd(cmdDef.Name, cmdDef))
            {
                _logger.LogWarning("命令冲突: 命令 '{Command}' 已被注册。", cmdDef.Name);
            }
        }
        Commands = tempMap;
        _logger.LogInformation("✅ [CommandRegistry] 成功发现并注册了 {Count} 个主命令。", Commands.Count);
    }
}