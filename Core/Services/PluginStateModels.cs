using qqbot.Services.Plugins;
using System.Reflection;

namespace qqbot.Core.Services;

/// <summary>
/// 插件加载状态
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
    public Dictionary<string, PythonProcessPoolState> ProcessPools { get; set; } = new();
    public List<PluginError> Errors { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Python进程池状态
/// </summary>
public class PythonProcessPoolState
{
    public string PoolKey { get; set; } = string.Empty;
    public int ActiveWorkers { get; set; }
    public int IdleWorkers { get; set; }
    public int TotalWorkers { get; set; }
    public bool IsHealthy { get; set; }
    public DateTime LastHealthCheck { get; set; }
    public List<string> WorkerIds { get; set; } = new();
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

/// <summary>
/// Python环境状态
/// </summary>
public class PythonEnvironmentState
{
    public string PluginId { get; set; } = string.Empty;
    public string EnvironmentPath { get; set; } = string.Empty;
    public string PythonExecutablePath { get; set; } = string.Empty;
    public bool IsSetup { get; set; }
    public DateTime LastSetupTime { get; set; }
    public List<string> InstalledPackages { get; set; } = new();
}
