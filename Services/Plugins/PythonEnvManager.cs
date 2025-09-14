using System.Diagnostics;

namespace qqbot.Services.Plugins;

/// <summary>
/// 负责为插件管理 Python 虚拟环境和依赖项。
/// </summary>
public class PythonEnvManager
{
    private readonly string _bootstrapEnvPath;
    private readonly string _bootstrapPythonExePath;
    private readonly string _bootstrapUvExePath;
    private readonly string _bootstrapPoetryExePath;
    private readonly ILogger<PythonEnvManager> _logger;

    public PythonEnvManager(ILogger<PythonEnvManager> logger)
    {
        _logger = logger;

        // 构造内嵌的“工具箱”环境的绝对路径
        _bootstrapEnvPath = Path.Combine(AppContext.BaseDirectory, "Python", ".bot","Scripts");
        _bootstrapPythonExePath = Path.Combine(_bootstrapEnvPath, "python.exe");
        _bootstrapUvExePath = Path.Combine(_bootstrapEnvPath, "uv.exe");
        _bootstrapPoetryExePath = Path.Combine(_bootstrapEnvPath, "poetry.exe");

        // 启动时进行一次检查，确保工具箱存在
        if (!File.Exists(_bootstrapPythonExePath) || !File.Exists(_bootstrapUvExePath) || !File.Exists(_bootstrapPoetryExePath))
        {
            var errorMsg = "核心工具 (python.exe, uv.exe, poetry.exe) 在内嵌的 Python 环境中未找到！";
            _logger.LogCritical(errorMsg);
            throw new FileNotFoundException(errorMsg);
        }
        _logger.LogInformation("Python 环境管理器初始化成功，已找到核心工具。");
    }

    /// <summary>
    /// 为指定的插件创建独立的 Python 虚拟环境并安装其依赖。
    /// 如果环境已存在，则跳过创建，只检查依赖。
    /// </summary>
    /// <returns>成功则返回该插件虚拟环境中的 python.exe 的路径，否则抛出异常。</returns>
    public string SetupEnvironmentForPlugin(string pluginDirectory, PythonToolInfo toolInfo)
    {
        var pluginId = Path.GetFileName(pluginDirectory);
        _logger.LogInformation("正在为插件 '{PluginId}' 准备 Python 环境...", pluginId);

        var venvPath = Path.Combine(pluginDirectory, ".venv");
        var pythonInVenvPath = Path.Combine(venvPath, "Scripts", "python.exe");

        // 使用内嵌的 uv 来创建虚拟环境，速度极快
        // 如果虚拟环境目录不存在，则创建它
        if (!Directory.Exists(venvPath))
        {
            _logger.LogInformation("为插件 '{PluginId}' 创建新的 Python 虚拟环境...", pluginId);
            ExecuteCommand(_bootstrapUvExePath, $"venv \"{venvPath}\" --python \"{_bootstrapPythonExePath}\"");
        }
        else
        {
            _logger.LogInformation("插件 '{PluginId}' 的虚拟环境已存在，检查依赖完整性...", pluginId);
        }

        var poetryProjectPath = Path.Combine(pluginDirectory, toolInfo.Script.Replace("main.py", ""));
        var poetryLockPath = Path.Combine(poetryProjectPath, "poetry.lock");

        // 优先使用 Poetry (如果存在 lock 文件)
        if (File.Exists(poetryLockPath))
        {
            _logger.LogInformation("发现 poetry.lock，使用 Poetry 安装/同步依赖...");
            // 在包含 pyproject.toml 的目录下执行 poetry install
            ExecuteCommandWithRetry(_bootstrapPoetryExePath, "install --no-interaction", poetryProjectPath, showDetailedLogs: true);
        }
        // 否则，使用 uv + requirements.txt
        else
        {
            var requirementsPath = toolInfo.Requirements;
            if (File.Exists(requirementsPath))
            {
                _logger.LogInformation("发现 requirements.txt，使用 uv 安装依赖...");
                _logger.LogInformation("依赖文件路径: {RequirementsPath}", requirementsPath);
                
                // 检查是否需要重新安装依赖
                if (ShouldReinstallDependencies(venvPath, requirementsPath))
                {
                    _logger.LogInformation("检测到依赖不完整或requirements.txt已更新，重新安装依赖...");
                    // 使用更详细的uv参数来显示安装进度
                    ExecuteCommandWithRetry(_bootstrapUvExePath, $"pip install -r \"{requirementsPath}\" --python \"{pythonInVenvPath}\" --verbose", showDetailedLogs: true);
                    
                    // 创建安装记录文件
                    CreateInstallRecord(venvPath, requirementsPath);
                }
                else
                {
                    _logger.LogInformation("依赖已完整，跳过安装");
                }
            }
            else
            {
                _logger.LogInformation("插件 '{PluginId}' 未提供依赖文件，无需安装。", pluginId);
            }
        }

        _logger.LogInformation("✅ 插件 '{PluginId}' 的 Python 环境已就绪！", pluginId);
        return pythonInVenvPath;
    }

    /// <summary>
    /// 带重试机制的命令执行
    /// </summary>
    private void ExecuteCommandWithRetry(string fileName, string arguments, string? workingDirectory = null, bool showDetailedLogs = false, int maxRetries = 3)
    {
        var lastException = (Exception?)null;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("尝试执行命令 (第 {Attempt}/{MaxRetries} 次): {FileName} {Arguments}", 
                    attempt, maxRetries, fileName, arguments);
                
                ExecuteCommand(fileName, arguments, workingDirectory, showDetailedLogs);
                
                if (attempt > 1)
                {
                    _logger.LogInformation("✅ 命令在第 {Attempt} 次尝试后成功执行", attempt);
                }
                return; // 成功执行，退出重试循环
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "第 {Attempt}/{MaxRetries} 次尝试失败", attempt, maxRetries);
                
                if (attempt < maxRetries)
                {
                    var delay = Math.Pow(2, attempt - 1) * 1000; // 指数退避: 1s, 2s, 4s
                    _logger.LogInformation("等待 {Delay}ms 后重试...", delay);
                    Thread.Sleep((int)delay);
                }
            }
        }
        
        // 所有重试都失败了
        _logger.LogError(lastException, "命令在 {MaxRetries} 次尝试后仍然失败", maxRetries);
        throw new Exception($"命令执行失败，已重试 {maxRetries} 次。最后一次错误: {lastException?.Message}", lastException);
    }

    /// <summary>
    /// 检查是否需要重新安装依赖
    /// </summary>
    private bool ShouldReinstallDependencies(string venvPath, string requirementsPath)
    {
        try
        {
            var pythonExe = Path.Combine(venvPath, "Scripts", "python.exe");
            if (!File.Exists(pythonExe))
            {
                _logger.LogInformation("Python可执行文件不存在，需要重新安装依赖");
                return true;
            }

            // 检查虚拟环境中是否有.installed文件
            var installedMarkerPath = Path.Combine(venvPath, ".installed");
            if (!File.Exists(installedMarkerPath))
            {
                _logger.LogInformation("未找到.installed标记文件，需要重新安装依赖");
                return true;
            }

            // 检查requirements.txt的最后修改时间
            var requirementsLastWrite = File.GetLastWriteTime(requirementsPath);
            var installedLastWrite = File.GetLastWriteTime(installedMarkerPath);
            
            if (requirementsLastWrite > installedLastWrite)
            {
                _logger.LogInformation("requirements.txt已更新，需要重新安装依赖");
                return true;
            }

            _logger.LogInformation("依赖已完整安装，无需重新安装");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "依赖验证过程中发生错误，将重新安装依赖");
            return true;
        }
    }

    /// <summary>
    /// 创建安装标记文件
    /// </summary>
    private void CreateInstallRecord(string venvPath, string requirementsPath)
    {
        try
        {
            var installedMarkerPath = Path.Combine(venvPath, ".installed");
            var recordContent = $@"# 依赖安装标记文件
        安装时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
        requirements.txt路径: {requirementsPath}
        requirements.txt最后修改时间: {File.GetLastWriteTime(requirementsPath):yyyy-MM-dd HH:mm:ss}
        虚拟环境路径: {venvPath}
        ";
            File.WriteAllText(installedMarkerPath, recordContent);
            _logger.LogInformation("✅ 已创建安装标记文件: {InstalledMarkerPath}", installedMarkerPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "创建安装标记文件失败");
        }
    }

    /// <summary>
    /// 执行一个外部命令并等待其完成
    /// </summary>
    private void ExecuteCommand(string fileName, string arguments, string? workingDirectory = null, bool showDetailedLogs = false)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
            }
        };

        _logger.LogInformation("执行命令: {FileName} {Arguments}", fileName, arguments);
        if (!string.IsNullOrEmpty(workingDirectory))
        {
            _logger.LogInformation("工作目录: {WorkingDirectory}", workingDirectory);
        }

        process.Start();

        if (showDetailedLogs)
        {
            // 实时读取输出，显示安装进度
            var outputTask = Task.Run(async () =>
            {
                using var reader = process.StandardOutput;
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _logger.LogInformation("📦 {Output}", line);
                    }
                }
            });

            var errorTask = Task.Run(async () =>
            {
                using var reader = process.StandardError;
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _logger.LogWarning("⚠️ {Error}", line);
                    }
                }
            });

            // 添加进度指示器
            var progressTask = Task.Run(async () =>
            {
                var dots = 0;
                while (!process.HasExited)
                {
                    await Task.Delay(2000); // 每2秒显示一次进度
                    if (!process.HasExited)
                    {
                        dots = (dots + 1) % 4;
                        var progress = new string('.', dots) + new string(' ', 3 - dots);
                        _logger.LogInformation("🔄 正在安装依赖{Progress}", progress);
                    }
                }
            });

            process.WaitForExit();
            
            // 等待输出读取完成
            Task.WaitAll(outputTask, errorTask, progressTask);
        }
        else
        {
            // 异步读取可以避免缓冲区满导致的死锁
            var stdOut = process.StandardOutput.ReadToEndAsync();
            var stdErr = process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            var output = stdOut.Result;
            var error = stdErr.Result;

            if (!string.IsNullOrWhiteSpace(output)) 
            {
                _logger.LogDebug("Command stdout: {StdOut}", output);
            }
            if (!string.IsNullOrWhiteSpace(error)) 
            {
                _logger.LogWarning("Command stderr: {StdErr}", error);
            }
        }

        if (process.ExitCode != 0)
        {
            var errorMessage = $"命令执行失败，退出码: {process.ExitCode}。命令: {fileName} {arguments}";
            throw new Exception(errorMessage);
        }
    }
}