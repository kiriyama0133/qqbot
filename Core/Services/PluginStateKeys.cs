namespace qqbot.Core.Services;

/// <summary>
/// 插件系统状态键常量
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
    /// 插件加载状态
    /// </summary>
    public const string PluginLoadingStatus = "Plugins.LoadingStatus";
    
    /// <summary>
    /// 插件错误状态
    /// </summary>
    public const string PluginErrors = "Plugins.Errors";
}

/// <summary>
/// 状态监控服务状态键常量
/// </summary>
public static class StateMonitorKeys
{
    /// <summary>
    /// 监控配置
    /// </summary>
    public const string MonitorConfig = "StateMonitor.Config";
    
    /// <summary>
    /// 监控状态
    /// </summary>
    public const string MonitorStatus = "StateMonitor.Status";
    
    /// <summary>
    /// 最后监控时间
    /// </summary>
    public const string LastMonitorTime = "StateMonitor.LastTime";
}