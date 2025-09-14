using System.Collections.Concurrent;

namespace qqbot.Services.Plugins;

/// <summary>
/// 监控Python进程池中所有进程的状态和健康情况
/// </summary>
public class PythonProcessMonitor : IDisposable
{
    private readonly ILogger<PythonProcessMonitor> _logger;
    private readonly Timer _monitorTimer;
    private readonly ConcurrentDictionary<string, PythonProcessWorker> _workers;
    private bool _disposed = false;

    public PythonProcessMonitor(ConcurrentDictionary<string, PythonProcessWorker> workers, ILogger<PythonProcessMonitor> logger)
    {
        _workers = workers ?? throw new ArgumentNullException(nameof(workers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // 每30秒检查一次进程状态
        _monitorTimer = new Timer(MonitorProcesses, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// 监控所有进程的状态
    /// </summary>
    private void MonitorProcesses(object? state)
    {
        if (_disposed) return;

        try
        {
            var unhealthyWorkers = new List<string>();
            var idleWorkers = new List<string>();

            foreach (var kvp in _workers)
            {
                var workerId = kvp.Key;
                var worker = kvp.Value;

                // 检查进程健康状态
                if (!worker.IsHealthy())
                {
                    unhealthyWorkers.Add(workerId);
                    _logger.LogWarning("检测到不健康的进程: {WorkerId} (PID: {ProcessId})", workerId, worker.Process.Id);
                }

                // 检查长时间空闲的进程
                var idleTime = worker.GetIdleTime();
                if (idleTime > TimeSpan.FromMinutes(10) && !worker.IsBusy)
                {
                    idleWorkers.Add(workerId);
                    _logger.LogDebug("检测到长时间空闲的进程: {WorkerId} (空闲时间: {IdleTime})", workerId, idleTime);
                }
            }

            // 记录监控统计信息
            if (unhealthyWorkers.Count > 0 || idleWorkers.Count > 0)
            {
                _logger.LogInformation("进程监控报告 - 不健康: {UnhealthyCount}, 长时间空闲: {IdleCount}, 总进程数: {TotalCount}",
                    unhealthyWorkers.Count, idleWorkers.Count, _workers.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "进程监控过程中发生错误");
        }
    }

    /// <summary>
    /// 获取进程池统计信息
    /// </summary>
    public ProcessPoolStats GetStats()
    {
        var totalWorkers = _workers.Count;
        var busyWorkers = _workers.Values.Count(w => w.IsBusy);
        var healthyWorkers = _workers.Values.Count(w => w.IsHealthy());
        var totalRequests = _workers.Values.Sum(w => w.RequestCount);

        var avgUptime = _workers.Values.Any() 
            ? TimeSpan.FromTicks((long)_workers.Values.Average(w => w.GetUptime().Ticks))
            : TimeSpan.Zero;

        return new ProcessPoolStats
        {
            TotalWorkers = totalWorkers,
            BusyWorkers = busyWorkers,
            IdleWorkers = totalWorkers - busyWorkers,
            HealthyWorkers = healthyWorkers,
            UnhealthyWorkers = totalWorkers - healthyWorkers,
            TotalRequests = totalRequests,
            AverageUptime = avgUptime
        };
    }

    /// <summary>
    /// 获取指定进程的详细信息
    /// </summary>
    public ProcessInfo? GetProcessInfo(string workerId)
    {
        if (_workers.TryGetValue(workerId, out var worker))
        {
            return new ProcessInfo
            {
                WorkerId = workerId,
                ProcessId = worker.Process.Id,
                IsBusy = worker.IsBusy,
                IsHealthy = worker.IsHealthy(),
                CreatedAt = worker.CreatedAt,
                LastUsedAt = worker.LastUsedAt,
                RequestCount = worker.RequestCount,
                Uptime = worker.GetUptime(),
                IdleTime = worker.GetIdleTime()
            };
        }
        return null;
    }

    /// <summary>
    /// 获取所有进程的详细信息
    /// </summary>
    public IEnumerable<ProcessInfo> GetAllProcessInfo()
    {
        return _workers.Select(kvp => new ProcessInfo
        {
            WorkerId = kvp.Key,
            ProcessId = kvp.Value.Process.Id,
            IsBusy = kvp.Value.IsBusy,
            IsHealthy = kvp.Value.IsHealthy(),
            CreatedAt = kvp.Value.CreatedAt,
            LastUsedAt = kvp.Value.LastUsedAt,
            RequestCount = kvp.Value.RequestCount,
            Uptime = kvp.Value.GetUptime(),
            IdleTime = kvp.Value.GetIdleTime()
        });
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _monitorTimer?.Dispose();
    }
}

/// <summary>
/// 进程池统计信息
/// </summary>
public record ProcessPoolStats
{
    public int TotalWorkers { get; init; }
    public int BusyWorkers { get; init; }
    public int IdleWorkers { get; init; }
    public int HealthyWorkers { get; init; }
    public int UnhealthyWorkers { get; init; }
    public int TotalRequests { get; init; }
    public TimeSpan AverageUptime { get; init; }
}

/// <summary>
/// 单个进程的详细信息
/// </summary>
public record ProcessInfo
{
    public string WorkerId { get; init; } = string.Empty;
    public int ProcessId { get; init; }
    public bool IsBusy { get; init; }
    public bool IsHealthy { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUsedAt { get; init; }
    public int RequestCount { get; init; }
    public TimeSpan Uptime { get; init; }
    public TimeSpan IdleTime { get; init; }
}
