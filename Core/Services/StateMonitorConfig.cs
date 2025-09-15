namespace qqbot.Core.Services;

/// <summary>
/// 状态监控配置
/// </summary>
public class StateMonitorConfig
{
    /// <summary>
    /// 是否启用状态监控
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 监控间隔（秒）
    /// </summary>
    public int IntervalSeconds { get; set; } = 20;

    /// <summary>
    /// 是否显示详细信息
    /// </summary>
    public bool ShowDetailedInfo { get; set; } = false;

    /// <summary>
    /// 是否只显示有变化的状态
    /// </summary>
    public bool OnlyShowChanges { get; set; } = false;

    /// <summary>
    /// 要监控的状态键列表（空表示监控所有）
    /// </summary>
    public List<string> MonitoredKeys { get; set; } = new();

    /// <summary>
    /// 要排除的状态键列表
    /// </summary>
    public List<string> ExcludedKeys { get; set; } = new();

    /// <summary>
    /// 最大显示的状态数量
    /// </summary>
    public int MaxDisplayCount { get; set; } = 50;
}
