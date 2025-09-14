using System.Collections.Concurrent;

namespace qqbot.Services.Plugins;

/// <summary>
/// 负责智能分配Python进程，处理负载均衡和阻塞检测
/// </summary>
public class PythonProcessAllocator
{
    private readonly ILogger<PythonProcessAllocator> _logger;
    private readonly ConcurrentDictionary<string, PythonProcessWorker> _workers;
    private readonly PythonProcessPoolSettings _settings;

    public PythonProcessAllocator(
        ConcurrentDictionary<string, PythonProcessWorker> workers,
        PythonProcessPoolSettings settings,
        ILogger<PythonProcessAllocator> logger)
    {
        _workers = workers ?? throw new ArgumentNullException(nameof(workers));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 尝试获取一个可用的进程工作器
    /// </summary>
    public async Task<PythonProcessWorker?> TryGetWorkerAsync(TimeSpan timeout = default)
    {
        if (timeout == default)
            timeout = _settings.RequestTimeout;

        var startTime = DateTime.Now;
        var attempts = 0;

        while (DateTime.Now - startTime < timeout)
        {
            attempts++;

            // 1. 尝试获取空闲的进程
            var idleWorker = await TryGetIdleWorkerAsync();
            if (idleWorker != null)
            {
                _logger.LogDebug("成功获取空闲进程 (尝试次数: {Attempts})", attempts);
                return idleWorker;
            }

            // 2. 检查是否可以创建新进程
            if (CanCreateNewWorker())
            {
                _logger.LogDebug("没有空闲进程，但可以创建新进程 (尝试次数: {Attempts})", attempts);
                return null; // 返回null表示需要创建新进程
            }

            // 3. 等待一段时间后重试
            var waitTime = Math.Min(100, (int)(timeout.TotalMilliseconds / 10));
            await Task.Delay(waitTime);
        }

        _logger.LogWarning("获取进程超时 (尝试次数: {Attempts}, 超时时间: {Timeout})", attempts, timeout);
        return null;
    }

    /// <summary>
    /// 尝试获取一个空闲的进程工作器
    /// </summary>
    private async Task<PythonProcessWorker?> TryGetIdleWorkerAsync()
    {
        // 按优先级排序：健康且空闲的进程优先
        var candidates = _workers.Values
            .Where(w => w.IsHealthy() && !w.IsBusy)
            .OrderBy(w => w.RequestCount) // 请求数少的优先
            .ThenBy(w => w.GetIdleTime()) // 空闲时间短的优先
            .ToList();

        foreach (var worker in candidates)
        {
            if (await worker.TryAcquireAsync(TimeSpan.FromMilliseconds(100)))
            {
                return worker;
            }
        }

        return null;
    }

    /// <summary>
    /// 检查是否可以创建新的工作进程
    /// </summary>
    public bool CanCreateNewWorker()
    {
        var currentCount = _workers.Count;
        var healthyCount = _workers.Values.Count(w => w.IsHealthy());

        // 检查最大进程数限制
        if (currentCount >= _settings.MaxPoolSize)
        {
            _logger.LogDebug("已达到最大进程数限制: {CurrentCount}/{MaxPoolSize}", currentCount, _settings.MaxPoolSize);
            return false;
        }

        // 检查健康进程数
        if (healthyCount >= _settings.MaxPoolSize)
        {
            _logger.LogDebug("健康进程数已达到限制: {HealthyCount}/{MaxPoolSize}", healthyCount, _settings.MaxPoolSize);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取负载均衡建议
    /// </summary>
    public LoadBalanceInfo GetLoadBalanceInfo()
    {
        var totalWorkers = _workers.Count;
        var busyWorkers = _workers.Values.Count(w => w.IsBusy);
        var healthyWorkers = _workers.Values.Count(w => w.IsHealthy());
        var idleWorkers = totalWorkers - busyWorkers;

        var loadPercentage = totalWorkers > 0 ? (double)busyWorkers / totalWorkers * 100 : 0;
        var healthPercentage = totalWorkers > 0 ? (double)healthyWorkers / totalWorkers * 100 : 0;

        var recommendation = GetRecommendation(loadPercentage, healthPercentage, totalWorkers);

        return new LoadBalanceInfo
        {
            TotalWorkers = totalWorkers,
            BusyWorkers = busyWorkers,
            IdleWorkers = idleWorkers,
            HealthyWorkers = healthyWorkers,
            LoadPercentage = loadPercentage,
            HealthPercentage = healthPercentage,
            Recommendation = recommendation,
            CanCreateNewWorker = CanCreateNewWorker()
        };
    }

    /// <summary>
    /// 获取负载均衡建议
    /// </summary>
    private LoadBalanceRecommendation GetRecommendation(double loadPercentage, double healthPercentage, int totalWorkers)
    {
        if (healthPercentage < 80)
        {
            return LoadBalanceRecommendation.CleanupUnhealthy;
        }

        if (loadPercentage > 90 && totalWorkers < _settings.MaxPoolSize)
        {
            return LoadBalanceRecommendation.CreateMoreWorkers;
        }

        if (loadPercentage < 20 && totalWorkers > _settings.MinPoolSize)
        {
            return LoadBalanceRecommendation.ReduceWorkers;
        }

        return LoadBalanceRecommendation.MaintainCurrent;
    }

    /// <summary>
    /// 释放工作进程
    /// </summary>
    public void ReleaseWorker(PythonProcessWorker worker)
    {
        if (worker != null)
        {
            worker.Release();
            _logger.LogDebug("工作进程已释放: {WorkerId}", worker.ProcessKey);
        }
    }
}

/// <summary>
/// 负载均衡信息
/// </summary>
public record LoadBalanceInfo
{
    public int TotalWorkers { get; init; }
    public int BusyWorkers { get; init; }
    public int IdleWorkers { get; init; }
    public int HealthyWorkers { get; init; }
    public double LoadPercentage { get; init; }
    public double HealthPercentage { get; init; }
    public LoadBalanceRecommendation Recommendation { get; init; }
    public bool CanCreateNewWorker { get; init; }
}

/// <summary>
/// 负载均衡建议
/// </summary>
public enum LoadBalanceRecommendation
{
    MaintainCurrent,      // 维持现状
    CreateMoreWorkers,    // 创建更多工作进程
    ReduceWorkers,        // 减少工作进程
    CleanupUnhealthy      // 清理不健康的进程
}

/// <summary>
/// Python进程池配置
/// </summary>
public class PythonProcessPoolSettings
{
    public int MinPoolSize { get; set; } = 1;
    public int MaxPoolSize { get; set; } = 5;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan WorkerIdleTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);
}
