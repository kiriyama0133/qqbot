namespace qqbot.Core.Services;

/// <summary>
/// 插件系统相关的状态键定义
/// </summary>
public static class PluginStateKeys
{
    /// <summary>
    /// 已发现的插件列表
    /// </summary>
    public const string DiscoveredPlugins = "Plugins.Discovered";
    
    /// <summary>
    /// 插件程序集列表
    /// </summary>
    public const string PluginAssemblies = "Plugins.Assemblies";
    
    /// <summary>
    /// Python环境管理器状态
    /// </summary>
    public const string PythonEnvManager = "Plugins.PythonEnvManager";
    
    /// <summary>
    /// Python进程管理器状态
    /// </summary>
    public const string PythonProcessManager = "Plugins.PythonProcessManager";
    
    /// <summary>
    /// Python进程池状态
    /// </summary>
    public const string PythonProcessPools = "Plugins.PythonProcessPools";
    
    /// <summary>
    /// 插件加载状态
    /// </summary>
    public const string PluginLoadingStatus = "Plugins.LoadingStatus";
    
    /// <summary>
    /// 插件错误状态
    /// </summary>
    public const string PluginErrors = "Plugins.Errors";
}

/// <summary>
/// 全局状态监控相关的状态键定义
/// </summary>
public static class StateMonitorKeys
{
    /// <summary>
    /// 状态监控配置
    /// </summary>
    public const string MonitorConfig = "StateMonitor.Config";
    
    /// <summary>
    /// 状态监控状态
    /// </summary>
    public const string MonitorStatus = "StateMonitor.Status";
    
    /// <summary>
    /// 最后监控时间
    /// </summary>
    public const string LastMonitorTime = "StateMonitor.LastTime";
}
