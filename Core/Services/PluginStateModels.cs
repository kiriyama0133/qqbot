using System.Reflection;
using qqbot.Services.Plugins;

namespace qqbot.Core.Services;

/// <summary>
/// 插件加载状态枚举
/// </summary>
public enum PluginLoadingStatus
{
    NotStarted,
    Discovering,
    Copying,
    Loading,
    Initializing,
    Completed,
    Failed
}

/// <summary>
/// 插件系统状态
/// </summary>
public class PluginSystemState
{
    public PluginLoadingStatus LoadingStatus { get; set; } = PluginLoadingStatus.NotStarted;
    public List<DiscoveredPlugin> DiscoveredPlugins { get; set; } = new();
    public List<Assembly> PluginAssemblies { get; set; } = new();
    public List<PluginError> Errors { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 插件错误信息
/// </summary>
public class PluginError
{
    public string PluginId { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}