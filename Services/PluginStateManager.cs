using qqbot.Core.Services;
using qqbot.Services.Plugins;
using qqbot.Helper;
using System.Reflection;

namespace qqbot.Services;

/// <summary>
/// 插件状态管理器，负责将插件系统的状态注册到全局状态管理中
/// </summary>
public class PluginStateManager
{
    private readonly IDynamicStateService _stateService;
    private readonly ILogger<PluginStateManager> _logger;
    private readonly PluginDiscoveryService _pluginDiscovery;
    private readonly PythonEnvManager _pythonEnvManager;
    private readonly PythonProcessManager _pythonProcessManager;

    public PluginStateManager(
        IDynamicStateService stateService,
        ILogger<PluginStateManager> logger,
        PluginDiscoveryService pluginDiscovery,
        PythonEnvManager pythonEnvManager,
        PythonProcessManager pythonProcessManager)
    {
        _stateService = stateService;
        _logger = logger;
        _pluginDiscovery = pluginDiscovery;
        _pythonEnvManager = pythonEnvManager;
        _pythonProcessManager = pythonProcessManager;
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

            var discoveredPlugins = await _pluginDiscovery.DiscoverPluginsAsync();
            pluginSystemState.DiscoveredPlugins = discoveredPlugins;
            pluginSystemState.LoadingStatus = PluginLoadingStatus.Copying;
            _stateService.SetState(PluginStateKeys.DiscoveredPlugins, pluginSystemState);

            // 发现插件程序集
            _logger.LogInformation("发现插件程序集...");
            var pluginAssemblies = PluginLoaderExtensions.DiscoverPluginAssemblies();
            pluginSystemState.PluginAssemblies = pluginAssemblies;
            pluginSystemState.LoadingStatus = PluginLoadingStatus.Loading;
            _stateService.SetState(PluginStateKeys.DiscoveredPlugins, pluginSystemState);

            // 初始化Python环境
            _logger.LogInformation("初始化Python环境...");
            await InitializePythonEnvironmentsAsync(discoveredPlugins);

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
    /// 初始化Python环境
    /// </summary>
    private async Task InitializePythonEnvironmentsAsync(List<DiscoveredPlugin> plugins)
    {
        var pythonPlugins = plugins.Where(p => p.Type == PluginType.Python || p.Type == PluginType.Hybrid);
        
        foreach (var plugin in pythonPlugins)
        {
            try
            {
                var envDirectory = Path.Combine(AppContext.BaseDirectory, "Envs", plugin.Id);
                Directory.CreateDirectory(envDirectory);
                
                var toolInfo = new PythonToolInfo
                {
                    Script = Path.GetFileName(plugin.MainScript ?? "main.py"),
                    Requirements = plugin.RequirementsFile ?? Path.Combine(plugin.SourceDirectory, "requirements.txt")
                };
                
                var pythonExePath = _pythonEnvManager.SetupEnvironmentForPlugin(envDirectory, toolInfo);
                
                // 更新环境状态
                var envState = new PythonEnvironmentState
                {
                    PluginId = plugin.Id,
                    EnvironmentPath = envDirectory,
                    PythonExecutablePath = pythonExePath,
                    IsSetup = true,
                    LastSetupTime = DateTime.UtcNow
                };
                
                _stateService.SetState($"{PluginStateKeys.PythonEnvManager}.{plugin.Id}", envState);
                _logger.LogInformation("✅ Python环境设置完成: {PluginId} -> {PythonPath}", plugin.Id, pythonExePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Python环境设置失败: {PluginId}", plugin.Id);
                
                // 记录错误状态
                var errorState = new PythonEnvironmentState
                {
                    PluginId = plugin.Id,
                    IsSetup = false,
                    LastSetupTime = DateTime.UtcNow
                };
                _stateService.SetState($"{PluginStateKeys.PythonEnvManager}.{plugin.Id}", errorState);
            }
        }
    }

    /// <summary>
    /// 更新进程池状态
    /// </summary>
    public void UpdateProcessPoolState(string poolKey, PythonProcessPoolState state)
    {
        _stateService.SetState($"{PluginStateKeys.PythonProcessPools}.{poolKey}", state);
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

    /// <summary>
    /// 获取Python环境状态
    /// </summary>
    public PythonEnvironmentState? GetPythonEnvironmentState(string pluginId)
    {
        return _stateService.GetState<PythonEnvironmentState>($"{PluginStateKeys.PythonEnvManager}.{pluginId}");
    }

    /// <summary>
    /// 获取进程池状态
    /// </summary>
    public PythonProcessPoolState? GetProcessPoolState(string poolKey)
    {
        return _stateService.GetState<PythonProcessPoolState>($"{PluginStateKeys.PythonProcessPools}.{poolKey}");
    }
}
