using System.Collections.Concurrent;
using System.Diagnostics;

namespace qqbot.Services.Plugins;

/// <summary>
/// Python进程池，管理多个Python进程工作器，提供负载均衡和阻塞检测
/// </summary>
public class PythonProcessPool : IDisposable
{
    private readonly ILogger<PythonProcessPool> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, PythonProcessWorker> _workers = new();
    private readonly PythonProcessAllocator _allocator;
    private readonly PythonProcessMonitor _monitor;
    private readonly PythonProcessPoolSettings _settings;
    private readonly SemaphoreSlim _creationSemaphore = new(1, 1);
    private bool _disposed = false;

    public PythonProcessPool(
        PythonProcessPoolSettings settings,
        ILogger<PythonProcessPool> logger,
        ILoggerFactory loggerFactory)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

        _allocator = new PythonProcessAllocator(_workers, _settings, 
            _loggerFactory.CreateLogger<PythonProcessAllocator>());
        _monitor = new PythonProcessMonitor(_workers, 
            _loggerFactory.CreateLogger<PythonProcessMonitor>());

        _logger.LogInformation("Python进程池已初始化 - 最小: {MinSize}, 最大: {MaxSize}", 
            _settings.MinPoolSize, _settings.MaxPoolSize);
    }

    /// <summary>
    /// 获取一个可用的进程工作器
    /// </summary>
    public async Task<PythonProcessWorker> GetWorkerAsync(
        string processKey,
        string pythonExecutablePath,
        string scriptPath,
        CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PythonProcessPool));

        _logger.LogDebug("请求获取进程工作器: {ProcessKey}", processKey);

        // 1. 尝试获取现有的空闲工作器
        var worker = await _allocator.TryGetWorkerAsync(_settings.RequestTimeout);
        if (worker != null)
        {
            _logger.LogDebug("成功获取现有工作器: {ProcessKey}", processKey);
            return worker;
        }

        // 2. 检查是否可以创建新的工作器
        if (!_allocator.CanCreateNewWorker())
        {
            _logger.LogWarning("无法获取工作器且无法创建新进程: {ProcessKey}", processKey);
            throw new InvalidOperationException($"无法为 {processKey} 获取进程工作器，进程池已满");
        }

        // 3. 创建新的工作器
        await _creationSemaphore.WaitAsync(cancellationToken);
        try
        {
            // 双重检查，防止并发创建
            worker = await _allocator.TryGetWorkerAsync(TimeSpan.FromMilliseconds(100));
            if (worker != null)
            {
                _logger.LogDebug("在创建过程中获取到现有工作器: {ProcessKey}", processKey);
                return worker;
            }

            worker = await CreateNewWorkerAsync(processKey, pythonExecutablePath, scriptPath);
            _logger.LogInformation("创建新的进程工作器: {ProcessKey} (PID: {ProcessId})", processKey, worker.Process.Id);
            return worker;
        }
        finally
        {
            _creationSemaphore.Release();
        }
    }

    /// <summary>
    /// 创建新的进程工作器
    /// </summary>
    private async Task<PythonProcessWorker> CreateNewWorkerAsync(
        string processKey,
        string pythonExecutablePath,
        string scriptPath)
    {
        var workerId = $"{processKey}-{Guid.NewGuid():N}";
        
        var startInfo = new ProcessStartInfo
        {
            FileName = pythonExecutablePath,
            Arguments = $"\"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
        };

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        
        if (!process.Start())
        {
            throw new Exception($"无法为 {processKey} 启动进程");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // 等待进程启动
        await Task.Delay(1000);
        if (process.HasExited)
        {
            throw new Exception($"进程启动后立即退出: {processKey}");
        }

        var worker = new PythonProcessWorker(process, workerId, 
            _loggerFactory.CreateLogger<PythonProcessWorker>());
        
        _workers[workerId] = worker;
        
        return worker;
    }

    /// <summary>
    /// 释放工作器回池中
    /// </summary>
    public void ReturnWorker(PythonProcessWorker worker)
    {
        if (worker == null) return;

        _allocator.ReleaseWorker(worker);
        _logger.LogDebug("工作器已返回池中: {WorkerId}", worker.ProcessKey);
    }

    /// <summary>
    /// 获取进程池统计信息
    /// </summary>
    public ProcessPoolStats GetStats()
    {
        return _monitor.GetStats();
    }

    /// <summary>
    /// 获取负载均衡信息
    /// </summary>
    public LoadBalanceInfo GetLoadBalanceInfo()
    {
        return _allocator.GetLoadBalanceInfo();
    }

    /// <summary>
    /// 获取所有进程的详细信息
    /// </summary>
    public IEnumerable<ProcessInfo> GetAllProcessInfo()
    {
        return _monitor.GetAllProcessInfo();
    }

    /// <summary>
    /// 清理不健康的工作器
    /// </summary>
    public Task<int> CleanupUnhealthyWorkersAsync()
    {
        var unhealthyWorkers = _workers.Values
            .Where(w => !w.IsHealthy())
            .ToList();

        var cleanedCount = 0;
        foreach (var worker in unhealthyWorkers)
        {
            if (_workers.TryRemove(worker.ProcessKey, out _))
            {
                worker.Dispose();
                cleanedCount++;
                _logger.LogInformation("清理不健康的工作器: {WorkerId}", worker.ProcessKey);
            }
        }

        if (cleanedCount > 0)
        {
            _logger.LogInformation("清理了 {Count} 个不健康的工作器", cleanedCount);
        }

        return Task.FromResult(cleanedCount);
    }

    /// <summary>
    /// 清理长时间空闲的工作器
    /// </summary>
    public Task<int> CleanupIdleWorkersAsync()
    {
        var idleWorkers = _workers.Values
            .Where(w => w.IsHealthy() && !w.IsBusy && w.GetIdleTime() > _settings.WorkerIdleTimeout)
            .Where(w => _workers.Count > _settings.MinPoolSize) // 确保不清理到最小数量以下
            .ToList();

        var cleanedCount = 0;
        foreach (var worker in idleWorkers)
        {
            if (_workers.Count <= _settings.MinPoolSize) break;

            if (_workers.TryRemove(worker.ProcessKey, out _))
            {
                worker.Dispose();
                cleanedCount++;
                _logger.LogInformation("清理空闲工作器: {WorkerId} (空闲时间: {IdleTime})", 
                    worker.ProcessKey, worker.GetIdleTime());
            }
        }

        if (cleanedCount > 0)
        {
            _logger.LogInformation("清理了 {Count} 个空闲工作器", cleanedCount);
        }

        return Task.FromResult(cleanedCount);
    }

    /// <summary>
    /// 获取活跃工作器数量
    /// </summary>
    public int GetActiveWorkerCount()
    {
        return _workers.Values.Count(w => w.IsBusy);
    }

    /// <summary>
    /// 获取空闲工作器数量
    /// </summary>
    public int GetIdleWorkerCount()
    {
        return _workers.Values.Count(w => !w.IsBusy && w.IsHealthy());
    }

    /// <summary>
    /// 获取总工作器数量
    /// </summary>
    public int GetTotalWorkerCount()
    {
        return _workers.Count;
    }

    /// <summary>
    /// 检查进程池是否健康
    /// </summary>
    public bool IsHealthy()
    {
        return _workers.Values.Any(w => w.IsHealthy());
    }

    /// <summary>
    /// 获取所有工作器ID
    /// </summary>
    public List<string> GetWorkerIds()
    {
        return _workers.Keys.ToList();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _logger.LogInformation("正在清理Python进程池...");

        // 清理所有工作器
        foreach (var worker in _workers.Values)
        {
            worker.Dispose();
        }
        _workers.Clear();

        // 清理资源
        _monitor.Dispose();
        _creationSemaphore.Dispose();

        _logger.LogInformation("Python进程池已清理完成");
    }
}
