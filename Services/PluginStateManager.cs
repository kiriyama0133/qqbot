using qqbot.Core.Services;
using qqbot.Services.Plugins;
using System.Reflection;

namespace qqbot.Services;

/// <summary>
/// 插件状态管理器 - 负责将插件系统的状态注册到全局状态管理中
/// </summary>
public class PluginStateManager
{
    private readonly IDynamicStateService _stateService;
    private readonly ILogger<PluginStateManager> _logger;
    private readonly PluginServiceRegistrar _pluginRegistrar;

    public PluginStateManager(
        IDynamicStateService stateService,
        ILogger<PluginStateManager> logger,
        PluginServiceRegistrar pluginRegistrar)
    {
        _stateService = stateService;
        _logger = logger;
        _pluginRegistrar = pluginRegistrar;
    }

    /// <summary>
    /// 初始化插件状态管理
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.LogInformation("开始初始化插件状态管理...");
        
        try
        {
            // 初始化插件系统状态
            var pluginSystemState = new PluginSystemState
            {
                LoadingStatus = PluginLoadingStatus.Discovering,
                LastUpdated = DateTime.UtcNow
            };
            _stateService.SetState(PluginStateKeys.DiscoveredPlugins, pluginSystemState);

            // 发现插件
            _logger.LogInformation("发现插件...");
            pluginSystemState.LoadingStatus = PluginLoadingStatus.Discovering;
            _stateService.SetState(PluginStateKeys.DiscoveredPlugins, pluginSystemState);

            var discoveredPlugins = await _pluginRegistrar.DiscoverPluginsAsync();
            pluginSystemState.DiscoveredPlugins = discoveredPlugins;
            pluginSystemState.LoadingStatus = PluginLoadingStatus.Loading;
            _stateService.SetState(PluginStateKeys.DiscoveredPlugins, pluginSystemState);

            // 加载插件程序集
            _logger.LogInformation("加载插件程序集...");
            var pluginAssemblies = _pluginRegistrar.LoadPluginAssemblies();
            pluginSystemState.PluginAssemblies = pluginAssemblies;
            pluginSystemState.LoadingStatus = PluginLoadingStatus.Loading;
            _stateService.SetState(PluginStateKeys.DiscoveredPlugins, pluginSystemState);

            // 更新最终状态
            pluginSystemState.LoadingStatus = PluginLoadingStatus.Completed;
            pluginSystemState.LastUpdated = DateTime.UtcNow;
            _stateService.SetState(PluginStateKeys.DiscoveredPlugins, pluginSystemState);

            _logger.LogInformation("✅ 插件状态管理初始化完成 - 发现 {PluginCount} 个插件，{AssemblyCount} 个程序集", 
                discoveredPlugins.Count, pluginAssemblies.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "插件状态管理初始化失败");
            
            // 设置错误状态
            var errorState = new PluginSystemState
            {
                LoadingStatus = PluginLoadingStatus.Failed,
                Errors = new List<PluginError>
                {
                    new PluginError
                    {
                        PluginId = "System",
                        ErrorType = "InitializationError",
                        Message = ex.Message,
                        StackTrace = ex.StackTrace ?? string.Empty
                    }
                },
                LastUpdated = DateTime.UtcNow
            };
            _stateService.SetState(PluginStateKeys.DiscoveredPlugins, errorState);
        }
    }


    /// <summary>
    /// 添加插件错误
    /// </summary>
    public void AddPluginError(string pluginId, Exception exception)
    {
        var error = new PluginError
        {
            PluginId = pluginId,
            ErrorType = exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace ?? string.Empty,
            Timestamp = DateTime.UtcNow
        };

        // 获取当前状态并添加错误
        var currentState = _stateService.GetState<PluginSystemState>(PluginStateKeys.DiscoveredPlugins, new PluginSystemState());
        currentState.Errors.Add(error);
        currentState.LastUpdated = DateTime.UtcNow;
        
        _stateService.SetState(PluginStateKeys.DiscoveredPlugins, currentState);
    }

    /// <summary>
    /// 获取插件系统状态
    /// </summary>
    public PluginSystemState GetPluginSystemState()
    {
        return _stateService.GetState<PluginSystemState>(PluginStateKeys.DiscoveredPlugins, new PluginSystemState());
    }


}
