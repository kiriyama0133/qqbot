using System.Collections.Concurrent;
using System.Diagnostics;

namespace qqbot.Services.Plugins;

/// <summary>
/// 兼容性记录，保持向后兼容
/// </summary>
public record PythonProcessInfo(Process Process);

/// <summary>
/// 负责 Python 进程生命周期管理的高级服务，使用进程池
/// </summary>
public class PythonProcessManager : IDisposable
{
    private readonly ILogger<PythonProcessManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, PythonProcessPool> _processPools = new();
    private readonly PythonProcessPoolSettings _defaultSettings;
    private bool _disposed = false;

    public PythonProcessManager(ILogger<PythonProcessManager> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _defaultSettings = new PythonProcessPoolSettings
        {
            MinPoolSize = 1,
            MaxPoolSize = 3,
            RequestTimeout = TimeSpan.FromSeconds(30),
            WorkerIdleTimeout = TimeSpan.FromMinutes(10),
            HealthCheckInterval = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// 确保一个由唯一键标识的进程正在运行（向后兼容方法）
    /// </summary>
    public async Task<PythonProcessInfo> EnsureProcessIsRunningAsync(
        string processKey,
        string pythonExecutablePath,
        string scriptPath)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PythonProcessManager));

        var worker = await GetWorkerAsync(processKey, pythonExecutablePath, scriptPath);
        return new PythonProcessInfo(worker.Process);
    }

    /// <summary>
    /// 获取一个进程工作器（新方法）
    /// </summary>
    public async Task<PythonProcessWorker> GetWorkerAsync(
        string processKey,
        string pythonExecutablePath,
        string scriptPath,
        CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PythonProcessManager));

        var pool = GetOrCreateProcessPool(processKey);
        return await pool.GetWorkerAsync(processKey, pythonExecutablePath, scriptPath, cancellationToken);
    }

    /// <summary>
    /// 释放工作器回池中
    /// </summary>
    public void ReturnWorker(string processKey, PythonProcessWorker worker)
    {
        if (_disposed || worker == null) return;

        if (_processPools.TryGetValue(processKey, out var pool))
        {
            pool.ReturnWorker(worker);
        }
    }

    /// <summary>
    /// 获取或创建进程池
    /// </summary>
    private PythonProcessPool GetOrCreateProcessPool(string processKey)
    {
        return _processPools.GetOrAdd(processKey, key =>
        {
            _logger.LogInformation("为进程键 '{ProcessKey}' 创建新的进程池", key);
            return new PythonProcessPool(_defaultSettings, _loggerFactory.CreateLogger<PythonProcessPool>(), _loggerFactory);
        });
    }

    /// <summary>
    /// 获取进程池统计信息
    /// </summary>
    public ProcessPoolStats GetStats(string processKey)
    {
        if (_processPools.TryGetValue(processKey, out var pool))
        {
            return pool.GetStats();
        }
        return new ProcessPoolStats();
    }

    /// <summary>
    /// 获取负载均衡信息
    /// </summary>
    public LoadBalanceInfo GetLoadBalanceInfo(string processKey)
    {
        if (_processPools.TryGetValue(processKey, out var pool))
        {
            return pool.GetLoadBalanceInfo();
        }
        return new LoadBalanceInfo();
    }

    /// <summary>
    /// 获取所有进程的详细信息
    /// </summary>
    public IEnumerable<ProcessInfo> GetAllProcessInfo(string processKey)
    {
        if (_processPools.TryGetValue(processKey, out var pool))
        {
            return pool.GetAllProcessInfo();
        }
        return Enumerable.Empty<ProcessInfo>();
    }

    /// <summary>
    /// 清理不健康的工作器
    /// </summary>
    public async Task<int> CleanupUnhealthyWorkersAsync(string processKey)
    {
        if (_processPools.TryGetValue(processKey, out var pool))
        {
            return await pool.CleanupUnhealthyWorkersAsync();
        }
        return 0;
    }

    /// <summary>
    /// 清理所有进程池中的不健康工作器
    /// </summary>
    public async Task<Dictionary<string, int>> CleanupAllUnhealthyWorkersAsync()
    {
        var results = new Dictionary<string, int>();
        
        foreach (var kvp in _processPools)
        {
            var processKey = kvp.Key;
            var pool = kvp.Value;
            var cleanedCount = await pool.CleanupUnhealthyWorkersAsync();
            results[processKey] = cleanedCount;
        }

        return results;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _logger.LogInformation("正在清理Python进程管理器...");

        foreach (var pool in _processPools.Values)
        {
            pool.Dispose();
        }
        _processPools.Clear();

        _logger.LogInformation("Python进程管理器已清理完成");
    }
}