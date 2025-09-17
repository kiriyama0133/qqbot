using qqbot.Core.Services;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Reactive.Subjects;

namespace qqbot.Services;

/// <summary>
/// 全局状态监控服务，定期打印和监控所有状态
/// </summary>
public class StateMonitorService : IHostedService, IDisposable
{
    private readonly IDynamicStateService _stateService;
    private readonly ILogger<StateMonitorService> _logger;
    private readonly Timer _monitorTimer;
    private readonly ConcurrentDictionary<string, object> _lastStates = new();
    private bool _disposed = false;

    public StateMonitorService(
        IDynamicStateService stateService,
        ILogger<StateMonitorService> logger)
    {
        _stateService = stateService;
        _logger = logger;

        // 初始化监控配置
        InitializeMonitorConfig();

        // 创建定时器，初始延迟1秒，然后按配置间隔执行
        var config = GetMonitorConfig();
        _monitorTimer = new Timer(MonitorStates, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(config.IntervalSeconds));
    }

    /// <summary>
    /// 初始化监控配置
    /// </summary>
    private void InitializeMonitorConfig()
    {
        var defaultConfig = new StateMonitorConfig
        {
            IsEnabled = true,
            IntervalSeconds = 30,
            ShowDetailedInfo = true,
            OnlyShowChanges = false,
            MonitoredKeys = new List<string>(),
            ExcludedKeys = new List<string>(),
            MaxDisplayCount = 50
        };

        _stateService.SetState(StateMonitorKeys.MonitorConfig, defaultConfig);
        _stateService.SetState(StateMonitorKeys.MonitorStatus, "Initialized");
        _stateService.SetState(StateMonitorKeys.LastMonitorTime, DateTime.UtcNow);
    }

    /// <summary>
    /// 获取监控配置
    /// </summary>
    public StateMonitorConfig GetMonitorConfig()
    {
        return _stateService.GetState<StateMonitorConfig>(StateMonitorKeys.MonitorConfig, new StateMonitorConfig());
    }

    /// <summary>
    /// 更新监控配置
    /// </summary>
    public void UpdateMonitorConfig(StateMonitorConfig config)
    {
        _stateService.SetState(StateMonitorKeys.MonitorConfig, config);
        
        // 更新定时器间隔
        if (!_disposed)
        {
            _monitorTimer.Change(TimeSpan.FromSeconds(config.IntervalSeconds), TimeSpan.FromSeconds(config.IntervalSeconds));
        }
        
        _logger.LogInformation("状态监控配置已更新: 启用={Enabled}, 间隔={Interval}s, 详细信息={Detailed}", 
            config.IsEnabled, config.IntervalSeconds, config.ShowDetailedInfo);
    }

    /// <summary>
    /// 启用/禁用监控
    /// </summary>
    public void SetMonitorEnabled(bool enabled)
    {
        var config = GetMonitorConfig();
        config.IsEnabled = enabled;
        UpdateMonitorConfig(config);
    }

    /// <summary>
    /// 设置监控间隔
    /// </summary>
    public void SetMonitorInterval(int intervalSeconds)
    {
        var config = GetMonitorConfig();
        config.IntervalSeconds = Math.Max(1, intervalSeconds); // 最小1秒
        UpdateMonitorConfig(config);
    }

    /// <summary>
    /// 监控状态的主方法
    /// </summary>
    private void MonitorStates(object? state)
    {
        try
        {
            var config = GetMonitorConfig();
            if (!config.IsEnabled)
            {
                return;
            }

            _stateService.SetState(StateMonitorKeys.LastMonitorTime, DateTime.UtcNow);
            _stateService.SetState(StateMonitorKeys.MonitorStatus, "Running");

            // 获取所有状态
            var allStates = GetAllStates();
            var filteredStates = FilterStates(allStates, config);

            if (filteredStates.Count == 0)
            {
                _logger.LogDebug("没有状态需要监控");
                return;
            }

            // 打印状态信息
            PrintStates(filteredStates, config);

            // 更新最后状态（用于变化检测）
            if (config.OnlyShowChanges)
            {
                foreach (var kvp in filteredStates)
                {
                    _lastStates[kvp.Key] = kvp.Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "状态监控过程中发生错误");
            _stateService.SetState(StateMonitorKeys.MonitorStatus, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取所有状态（通过反射访问内部状态）
    /// </summary>
    private Dictionary<string, object> GetAllStates()
    {
        var states = new Dictionary<string, object>();

        try
        {
            // 通过反射访问DynamicStateService的内部状态
            var stateServiceType = _stateService.GetType();
            var stateSubjectsField = stateServiceType.GetField("_stateSubjects", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (stateSubjectsField?.GetValue(_stateService) is ConcurrentDictionary<string, ISubject<object>> stateSubjects)
            {
                foreach (var kvp in stateSubjects)
                {
                    if (kvp.Value is BehaviorSubject<object> behaviorSubject && behaviorSubject.TryGetValue(out var value))
                    {
                        states[kvp.Key] = value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "无法通过反射获取所有状态，使用备用方法");
            
            // 备用方法：手动获取已知状态
            states["Commands.Map"] = _stateService.GetState<object>("Commands.Map");
            states[StateMonitorKeys.MonitorConfig] = _stateService.GetState<object>(StateMonitorKeys.MonitorConfig);
            states[StateMonitorKeys.MonitorStatus] = _stateService.GetState<object>(StateMonitorKeys.MonitorStatus);
            states[StateMonitorKeys.LastMonitorTime] = _stateService.GetState<object>(StateMonitorKeys.LastMonitorTime);
        }

        return states;
    }

    /// <summary>
    /// 过滤状态
    /// </summary>
    private Dictionary<string, object> FilterStates(Dictionary<string, object> allStates, StateMonitorConfig config)
    {
        var filtered = new Dictionary<string, object>();

        foreach (var kvp in allStates)
        {
            // 检查是否在排除列表中
            if (config.ExcludedKeys.Contains(kvp.Key))
                continue;

            // 检查是否在监控列表中（如果指定了监控列表）
            if (config.MonitoredKeys.Count > 0 && !config.MonitoredKeys.Contains(kvp.Key))
                continue;

            // 检查是否只显示变化
            if (config.OnlyShowChanges)
            {
                if (_lastStates.TryGetValue(kvp.Key, out var lastValue))
                {
                    if (AreValuesEqual(kvp.Value, lastValue))
                        continue;
                }
            }

            filtered[kvp.Key] = kvp.Value;
        }

        // 限制显示数量
        if (filtered.Count > config.MaxDisplayCount)
        {
            var limited = new Dictionary<string, object>();
            var count = 0;
            foreach (var kvp in filtered)
            {
                if (count >= config.MaxDisplayCount) break;
                limited[kvp.Key] = kvp.Value;
                count++;
            }
            return limited;
        }

        return filtered;
    }

    /// <summary>
    /// 比较两个值是否相等
    /// </summary>
    private bool AreValuesEqual(object? value1, object? value2)
    {
        if (value1 == null && value2 == null) return true;
        if (value1 == null || value2 == null) return false;
        
        try
        {
            var json1 = JsonSerializer.Serialize(value1);
            var json2 = JsonSerializer.Serialize(value2);
            return json1 == json2;
        }
        catch
        {
            return value1.Equals(value2);
        }
    }

    /// <summary>
    /// 打印状态信息
    /// </summary>
    private void PrintStates(Dictionary<string, object> states, StateMonitorConfig config)
    {
        if (states.Count == 0) return;

        _logger.LogInformation("=== 全局状态监控 ({Count} 个状态) ===", states.Count);

        foreach (var kvp in states)
        {
            try
            {
                var valueStr = FormatValue(kvp.Value, config.ShowDetailedInfo);
                _logger.LogInformation("  {Key}: {Value}", kvp.Key, valueStr);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("无法格式化状态 {Key}: {Error}", kvp.Key, ex.Message);
            }
        }

        _logger.LogInformation("=== 状态监控完成 ===");
    }

    /// <summary>
    /// 格式化值显示
    /// </summary>
    private string FormatValue(object? value, bool showDetailed)
    {
        if (value == null) return "null";

        if (value is string str)
        {
            return showDetailed ? $"\"{str}\"" : str;
        }

        if (value is DateTime dt)
        {
            return showDetailed ? dt.ToString("yyyy-MM-dd HH:mm:ss.fff") : dt.ToString("HH:mm:ss");
        }

        if (value is TimeSpan ts)
        {
            return showDetailed ? ts.ToString(@"hh\:mm\:ss\.fff") : ts.ToString(@"mm\:ss");
        }

        if (value is System.Collections.ICollection collection)
        {
            var count = collection.Count;
            if (showDetailed && count <= 10)
            {
                var items = collection.Cast<object>().Select(item => FormatValue(item, false));
                return $"[{count}] {{{string.Join(", ", items)}}}";
            }
            return $"[{count} items]";
        }

        // 处理复杂对象
        if (showDetailed)
        {
            try
            {
                // 使用缩进格式显示JSON
                var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                });
                
                // 如果JSON太长，截断显示
                if (json.Length > 500)
                {
                    return json.Substring(0, 500) + "... (truncated)";
                }
                
                return json;
            }
            catch
            {
                // 如果JSON序列化失败，尝试使用反射显示属性
                return FormatObjectProperties(value);
            }
        }

        return value.ToString() ?? "null";
    }

    /// <summary>
    /// 格式化对象属性
    /// </summary>
    private string FormatObjectProperties(object obj)
    {
        try
        {
            var type = obj.GetType();
            var properties = type.GetProperties()
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .Take(5) // 只显示前5个属性
                .ToList();

            if (properties.Count == 0)
            {
                return obj.ToString() ?? "null";
            }

            var propertyStrings = properties.Select(p =>
            {
                try
                {
                    var val = p.GetValue(obj);
                    var valStr = val?.ToString() ?? "null";
                    if (valStr.Length > 50)
                    {
                        valStr = valStr.Substring(0, 50) + "...";
                    }
                    return $"{p.Name}: {valStr}";
                }
                catch
                {
                    return $"{p.Name}: <error>";
                }
            });

            return $"{{{string.Join(", ", propertyStrings)}}}";
        }
        catch
        {
            return obj.ToString() ?? "null";
        }
    }

    /// <summary>
    /// 手动触发状态监控
    /// </summary>
    public void TriggerMonitor()
    {
        MonitorStates(null);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("状态监控服务已启动，默认间隔: {Interval}秒", GetMonitorConfig().IntervalSeconds);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("状态监控服务已停止");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        _monitorTimer?.Dispose();
        _stateService.SetState(StateMonitorKeys.MonitorStatus, "Stopped");
    }
}
