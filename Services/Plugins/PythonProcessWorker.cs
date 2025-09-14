using System.Diagnostics;

namespace qqbot.Services.Plugins;

/// <summary>
/// 表示一个Python进程工作器，封装进程状态和操作
/// </summary>
public class PythonProcessWorker : IDisposable
{
    private readonly ILogger<PythonProcessWorker> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed = false;

    public Process Process { get; }
    public string ProcessKey { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastUsedAt { get; private set; }
    public bool IsBusy { get; private set; }
    public int RequestCount { get; private set; }

    public PythonProcessWorker(Process process, string processKey, ILogger<PythonProcessWorker> logger)
    {
        Process = process ?? throw new ArgumentNullException(nameof(process));
        ProcessKey = processKey ?? throw new ArgumentNullException(nameof(processKey));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        CreatedAt = DateTime.Now;
        LastUsedAt = DateTime.Now;
        IsBusy = false;
        RequestCount = 0;

        // 监听进程退出事件
        process.Exited += OnProcessExited;
    }

    /// <summary>
    /// 尝试获取进程使用权，如果进程忙碌则返回false
    /// </summary>
    public async Task<bool> TryAcquireAsync(TimeSpan timeout = default)
    {
        if (_disposed) return false;
        if (Process.HasExited) return false;

        var acquired = await _semaphore.WaitAsync(timeout);
        if (acquired)
        {
            IsBusy = true;
            LastUsedAt = DateTime.Now;
            RequestCount++;
            _logger.LogDebug("进程 {ProcessKey} (PID: {ProcessId}) 被获取", ProcessKey, Process.Id);
        }
        return acquired;
    }

    /// <summary>
    /// 释放进程使用权
    /// </summary>
    public void Release()
    {
        if (_disposed) return;

        IsBusy = false;
        _semaphore.Release();
        _logger.LogDebug("进程 {ProcessKey} (PID: {ProcessId}) 被释放", ProcessKey, Process.Id);
    }

    /// <summary>
    /// 检查进程是否健康（未退出且响应正常）
    /// </summary>
    public bool IsHealthy()
    {
        if (_disposed) return false;
        if (Process.HasExited) return false;

        // 可以添加更多健康检查逻辑，比如ping进程
        return true;
    }

    /// <summary>
    /// 获取进程空闲时间
    /// </summary>
    public TimeSpan GetIdleTime()
    {
        return DateTime.Now - LastUsedAt;
    }

    /// <summary>
    /// 获取进程运行时间
    /// </summary>
    public TimeSpan GetUptime()
    {
        return DateTime.Now - CreatedAt;
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        _logger.LogWarning("进程 {ProcessKey} (PID: {ProcessId}) 已退出", ProcessKey, Process.Id);
        IsBusy = false;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        IsBusy = false;
        
        try
        {
            if (!Process.HasExited)
            {
                Process.Kill();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "终止进程 {ProcessKey} (PID: {ProcessId}) 时发生错误", ProcessKey, Process.Id);
        }

        _semaphore.Dispose();
        Process.Dispose();
    }
}
