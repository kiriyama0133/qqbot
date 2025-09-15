namespace qqbot.Abstractions;

/// <summary>
/// 插件服务接口，所有插件服务都应该实现此接口
/// </summary>
public interface IPluginService
{
    /// <summary>
    /// 获取服务名称
    /// </summary>
    string ServiceName { get; }
    
    /// <summary>
    /// 获取服务版本
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// 初始化服务
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// 清理服务资源
    /// </summary>
    Task DisposeAsync();
}
