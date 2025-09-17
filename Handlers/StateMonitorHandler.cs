using MediatR;
using qqbot.Abstractions;
using qqbot.Core.Services;
using qqbot.Models;
using qqbot.Models.Notifications;
using qqbot.Services;
using qqbot.Services.Plugins;
using System.Diagnostics;
using System.Text.Json;

namespace qqbot.Handlers;

/// <summary>
/// 状态监控控制命令处理器
/// </summary>
public class StateMonitorHandler : 
    INotificationHandler<GroupMessageReceivedNotification>,
    INotificationHandler<PrivateMessageReceivedNotification>,
    ICommandHandler
{
    private readonly ILogger<StateMonitorHandler> _logger;
    private readonly StateMonitorService _stateMonitorService;
    private readonly IDynamicStateService _stateService;
    private readonly NapCatApiService _napCatApiService;
    private readonly PythonProcessManager _pythonProcessManager;

    public CommandDefinition Command { get; } = new()
    {
        Name = "/monitor",
        Description = "控制全局状态监控服务",
        Aliases = new List<string> { "/状态监控", "/监控" },
        SubCommands = new List<CommandDefinition>
        {
            new CommandDefinition
            {
                Name = "status",
                Description = "查看监控状态",
                Aliases = new List<string> { "状态" }
            },
            new CommandDefinition
            {
                Name = "enable",
                Description = "启用监控",
                Aliases = new List<string> { "开启", "on" }
            },
            new CommandDefinition
            {
                Name = "disable",
                Description = "禁用监控",
                Aliases = new List<string> { "关闭", "off" }
            },
            new CommandDefinition
            {
                Name = "interval",
                Description = "设置监控间隔（秒）",
                Aliases = new List<string> { "间隔", "time" },
                Arguments = new List<CommandArgument>
                {
                    new CommandArgument
                    {
                        Name = "seconds",
                        Description = "监控间隔秒数（最小1秒）",
                        IsRequired = true
                    }
                }
            },
            new CommandDefinition
            {
                Name = "trigger",
                Description = "手动触发一次监控",
                Aliases = new List<string> { "触发", "now" }
            },
            new CommandDefinition
            {
                Name = "config",
                Description = "查看监控配置",
                Aliases = new List<string> { "配置", "settings" }
            },
            new CommandDefinition
            {
                Name = "image",
                Description = "获取进程监控状况图片",
                Aliases = new List<string> { "图片", "图", "render" }
            }
        }
    };

    public StateMonitorHandler(
        ILogger<StateMonitorHandler> logger,
        StateMonitorService stateMonitorService,
        IDynamicStateService stateService,
        NapCatApiService napCatApiService,
        PythonProcessManager pythonProcessManager)
    {
        _logger = logger;
        _stateMonitorService = stateMonitorService;
        _stateService = stateService;
        _napCatApiService = napCatApiService;
        _pythonProcessManager = pythonProcessManager;
    }

    public async Task Handle(GroupMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var messageEvent = notification.MessageEvent;
        if (IsMonitorCommand(messageEvent.RawMessage))
        {
            var (message, imagePath) = await ProcessMonitorCommand(messageEvent.RawMessage);
            var replyMessage = new List<MessageSegment> { MessageSegment.Text(message) };
            
            // 如果有图片路径，添加图片消息段
            if (!string.IsNullOrEmpty(imagePath))
            {
                replyMessage.Add(MessageSegment.Image(file: imagePath, url: ""));
            }
            
            await _napCatApiService.SendGroupMessageAsync(messageEvent.GroupId, replyMessage);
        }
    }

    public async Task Handle(PrivateMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var messageEvent = notification.MessageEvent;
        if (IsMonitorCommand(messageEvent.RawMessage))
        {
            var (message, imagePath) = await ProcessMonitorCommand(messageEvent.RawMessage);
            var replyMessage = new List<MessageSegment> { MessageSegment.Text(message) };
            
            // 如果有图片路径，添加图片消息段
            if (!string.IsNullOrEmpty(imagePath))
            {
                replyMessage.Add(MessageSegment.Image(file: imagePath, url: ""));
            }
            
            await _napCatApiService.SendPrivateMessageAsync(messageEvent.UserId, replyMessage);
        }
    }

    private bool IsMonitorCommand(string message)
    {
        var commandText = message.Trim();
        return commandText.StartsWith(Command.Name, StringComparison.OrdinalIgnoreCase) ||
               Command.Aliases.Any(alias => commandText.StartsWith(alias, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<(string Message, string? ImagePath)> ProcessMonitorCommand(string message)
    {
        try
        {
            var parts = message.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return ("用法: /monitor <子命令>\n可用子命令: status, enable, disable, interval, trigger, config, image", null);
            }

            var subCommand = parts[1].ToLower();
            return subCommand switch
            {
                "status" or "状态" => (GetMonitorStatus(), null),
                "enable" or "开启" or "on" => (EnableMonitor(), null),
                "disable" or "关闭" or "off" => (DisableMonitor(), null),
                "interval" or "间隔" or "time" => (SetMonitorInterval(parts), null),
                "trigger" or "触发" or "now" => (TriggerMonitor(), null),
                "config" or "配置" or "settings" => (GetMonitorConfig(), null),
                "image" or "图片" or "图" or "render" => await GetMonitorImage(),
                _ => ("未知子命令。可用命令: status, enable, disable, interval, trigger, config, image", null)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理监控命令时发生错误");
            return ($"处理命令时发生错误: {ex.Message}", null);
        }
    }

    private string GetMonitorStatus()
    {
        var config = _stateMonitorService.GetMonitorConfig();
        var status = _stateService.GetState<string>(StateMonitorKeys.MonitorStatus, "Unknown");
        var lastTime = _stateService.GetState<DateTime?>(StateMonitorKeys.LastMonitorTime);

        var statusText = $"状态监控状态\n" +
                        $"启用: {(config.IsEnabled ? "是" : "否")}\n" +
                        $"间隔: {config.IntervalSeconds}秒\n" +
                        $"状态: {status}\n" +
                        $"最后监控: {lastTime?.ToString("HH:mm:ss") ?? "从未"}";

        return statusText;
    }

    private string EnableMonitor()
    {
        _stateMonitorService.SetMonitorEnabled(true);
        return "状态监控已启用";
    }

    private string DisableMonitor()
    {
        _stateMonitorService.SetMonitorEnabled(false);
        return "状态监控已禁用";
    }

    private string SetMonitorInterval(string[] parts)
    {
        if (parts.Length < 3)
        {
            return "用法: /monitor interval <秒数>";
        }

        if (!int.TryParse(parts[2], out var seconds) || seconds < 1)
        {
            return "错误: 间隔必须是大于0的整数";
        }

        _stateMonitorService.SetMonitorInterval(seconds);
        return $"监控间隔已设置为 {seconds} 秒";
    }

    private string TriggerMonitor()
    {
        _stateMonitorService.TriggerMonitor();
        return "已手动触发状态监控";
    }

    private string GetMonitorConfig()
    {
        var config = _stateMonitorService.GetMonitorConfig();
        
        var configText = $"监控配置\n" +
                        $"启用: {(config.IsEnabled ? "是" : "否")}\n" +
                        $"间隔: {config.IntervalSeconds}秒\n" +
                        $"详细信息: {(config.ShowDetailedInfo ? "是" : "否")}\n" +
                        $"仅显示变化: {(config.OnlyShowChanges ? "是" : "否")}\n" +
                        $"最大显示数: {config.MaxDisplayCount}\n" +
                        $"监控键数: {config.MonitoredKeys.Count}\n" +
                        $"排除键数: {config.ExcludedKeys.Count}";

        return configText;
    }

    private async Task<(string Message, string? ImagePath)> GetMonitorImage()
    {
        try
        {
            var pluginId = "process-monitor-renderer";
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "ExtensionsEntry", pluginId, "main.py");
            
            if (!File.Exists(scriptPath))
            {
                return ("进程监控渲染器插件未找到，请确保插件已正确安装", null);
            }

            // 获取Python可执行文件路径
            var pythonExePath = GetPythonExecutablePath(pluginId);
            if (string.IsNullOrEmpty(pythonExePath))
            {
                return ("未找到Python可执行文件，请检查插件环境配置", null);
            }

            // 获取插件状态信息
            var pluginStateJson = GetPluginStateJson();
            
            // 使用PythonProcessManager执行脚本（复用进程池）
            var result = await ExecutePythonScriptWithPoolAsync(pluginId, pythonExePath, scriptPath, pluginStateJson);
            
            if (result.Success && !string.IsNullOrEmpty(result.Output))
            {
                try
                {
                    // 解析JSON输出
                    var jsonResult = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(result.Output);
                    
                    if (jsonResult != null && jsonResult.ContainsKey("success") && 
                        jsonResult["success"].ToString() == "True")
                    {
                        var imagePath = jsonResult.ContainsKey("image_path") ? jsonResult["image_path"].ToString() : "";
                        var message = jsonResult.ContainsKey("message") ? jsonResult["message"].ToString() : "图片生成成功";
                        
                        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                        {
                            return ($"[成功] {message}", imagePath);
                        }
                        else
                        {
                            return ($"[警告] {message}，但图片文件未找到", null);
                        }
                    }
                    else
                    {
                        var errorMessage = jsonResult?.ContainsKey("message") == true ? 
                            jsonResult["message"].ToString() : "未知错误";
                        return ($"[错误] 图片生成失败: {errorMessage}", null);
                    }
                }
                catch (Exception jsonEx)
                {
                    _logger.LogError(jsonEx, "解析Python脚本输出时发生错误: {Output}", result.Output);
                    return ($"[警告] 图片生成完成，但解析结果时出错: {jsonEx.Message}", null);
                }
            }
            else
            {
                var errorMsg = result.Error ?? "未知错误";
                return ($"[错误] 执行Python脚本失败: {errorMsg}", null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成监控图片时发生错误");
            return ($"生成图片时发生错误: {ex.Message}", null);
        }
    }

    private async Task<(bool Success, string Output, string Error)> ExecutePythonScriptWithPoolAsync(string pluginId, string pythonExePath, string scriptPath, string? pluginStateJson = null)
    {
        try
        {
            // 由于PythonProcessPool的进程已经使用了异步输出流，我们回退到直接执行方式
            // 这样可以避免混合同步和异步操作的问题
            return await ExecutePythonScriptAsync(pythonExePath, scriptPath, pluginStateJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "使用进程池执行Python脚本时发生异常");
            return (false, "", ex.Message);
        }
    }

    private async Task<(bool Success, string Output, string Error)> ExecutePythonScriptAsync(string pythonExePath, string scriptPath, string? pluginStateJson = null)
    {
        string? tempJsonFile = null;
        try
        {
            var arguments = $"\"{scriptPath}\"";
            
            if (!string.IsNullOrEmpty(pluginStateJson))
            {
                // 创建临时JSON文件来传递插件状态数据
                tempJsonFile = Path.Combine(Path.GetTempPath(), $"plugin_state_{Guid.NewGuid():N}.json");
                await File.WriteAllTextAsync(tempJsonFile, pluginStateJson, System.Text.Encoding.UTF8);
                arguments += $" \"{tempJsonFile}\"";
                
                _logger.LogDebug("创建临时JSON文件: {TempFile}", tempJsonFile);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = pythonExePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                WorkingDirectory = Path.GetDirectoryName(scriptPath)
            };

            using var process = new Process { StartInfo = startInfo };
            
            _logger.LogDebug("执行Python脚本: {PythonExe} {ScriptPath}", pythonExePath, scriptPath);
            
            process.Start();
            
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            var output = await outputTask;
            var error = await errorTask;
            
            if (process.ExitCode == 0)
            {
                return (true, output, error);
            }
            else
            {
                _logger.LogError("Python脚本执行失败，退出码: {ExitCode}, 错误: {Error}", process.ExitCode, error);
                return (false, output, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行Python脚本时发生异常");
            return (false, "", ex.Message);
        }
        finally
        {
            // 清理临时文件
            if (!string.IsNullOrEmpty(tempJsonFile) && File.Exists(tempJsonFile))
            {
                try
                {
                    File.Delete(tempJsonFile);
                    _logger.LogDebug("删除临时JSON文件: {TempFile}", tempJsonFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "删除临时JSON文件失败: {TempFile}", tempJsonFile);
                }
            }
        }
    }

    private string GetPluginStateJson()
    {
        try
        {
            // 从状态服务获取插件信息
            var pluginSystemState = _stateService.GetState<PluginSystemState>(PluginStateKeys.DiscoveredPlugins);
            
            _logger.LogInformation("获取插件系统状态: LoadingStatus={LoadingStatus}, DiscoveredPluginsCount={Count}", 
                pluginSystemState?.LoadingStatus, pluginSystemState?.DiscoveredPlugins?.Count ?? 0);
            
            if (pluginSystemState?.DiscoveredPlugins == null)
            {
                _logger.LogWarning("插件系统状态为空或未发现插件");
                return "{}";
            }

            // 构建插件状态信息
            var activePlugins = new List<object>();
            var allProcessPools = new List<PythonProcessPoolState>();
            
            foreach (var plugin in pluginSystemState.DiscoveredPlugins.Where(p => p.Type == PluginType.Python))
            {
                // 获取该插件的进程池状态
                var poolStateKey = $"{PluginStateKeys.PythonProcessPools}.{plugin.Id}";
                var pluginPoolState = _stateService.GetState<PythonProcessPoolState>(poolStateKey);
                
                // 详细记录插件元数据
                _logger.LogInformation("处理插件: Id={PluginId}, Name={PluginName}, Type={PluginType}, Version={Version}, Description={Description}, Author={Author}", 
                    plugin.Id, plugin.Name, plugin.Type, plugin.Version, plugin.Description, plugin.Author);
                
                _logger.LogInformation("插件文件信息: MainScript={MainScript}, PythonFiles={PythonCount}, DllFiles={DllCount}, ConfigFiles={ConfigCount}, Dependencies={DependencyCount}", 
                    plugin.MainScript, plugin.PythonFiles?.Length ?? 0, plugin.DllFiles?.Length ?? 0, 
                    plugin.ConfigFiles?.Length ?? 0, plugin.Dependencies?.Length ?? 0);
                
                if (pluginPoolState != null)
                {
                    allProcessPools.Add(pluginPoolState);
                    _logger.LogInformation("插件进程池状态: PoolKey={PoolKey}, TotalWorkers={TotalWorkers}, ActiveWorkers={ActiveWorkers}, IdleWorkers={IdleWorkers}, IsHealthy={IsHealthy}", 
                        pluginPoolState.PoolKey, pluginPoolState.TotalWorkers, pluginPoolState.ActiveWorkers, 
                        pluginPoolState.IdleWorkers, pluginPoolState.IsHealthy);
                }
                else
                {
                    _logger.LogWarning("插件 {PluginId} 没有找到进程池状态", plugin.Id);
                }
                
                // 即使没有进程池状态，也要使用真实的插件元数据
                var isRunning = pluginPoolState != null && pluginPoolState.ActiveWorkers > 0;
                var processCount = pluginPoolState?.ActiveWorkers ?? 0;
                
                // 如果没有进程池状态，但插件存在，我们仍然应该显示插件信息
                // 只是标记为"空闲"状态
                
                activePlugins.Add(new
                {
                    name = plugin.Id,
                    display_name = plugin.Name ?? plugin.Id,
                    status = isRunning ? "running" : "idle",
                    processes = processCount,
                    type = plugin.Type.ToString(),
                    version = plugin.Version ?? "未知版本",
                    description = plugin.Description ?? "无描述",
                    author = plugin.Author ?? "未知作者",
                    main_script = plugin.MainScript ?? "无主脚本",
                    python_files_count = plugin.PythonFiles?.Length ?? 0,
                    dll_files_count = plugin.DllFiles?.Length ?? 0,
                    config_files_count = plugin.ConfigFiles?.Length ?? 0,
                    dependencies = plugin.Dependencies ?? Array.Empty<string>(),
                    pool_info = pluginPoolState != null ? new
                    {
                        total_workers = pluginPoolState.TotalWorkers,
                        active_workers = pluginPoolState.ActiveWorkers,
                        idle_workers = pluginPoolState.IdleWorkers,
                        is_healthy = pluginPoolState.IsHealthy
                    } : null
                });
            }

            var runningPlugins = activePlugins.Count(p => ((dynamic)p).status == "running");
            var idlePlugins = activePlugins.Count(p => ((dynamic)p).status == "idle");
            
            var pluginStatus = new
            {
                total_plugins = pluginSystemState.DiscoveredPlugins.Count,
                running_plugins = runningPlugins,
                idle_plugins = idlePlugins,
                active_plugins = activePlugins.ToArray(),
                system_info = new
                {
                    total_process_pools = allProcessPools.Count,
                    total_workers = allProcessPools.Sum(p => p.TotalWorkers),
                    active_workers = allProcessPools.Sum(p => p.ActiveWorkers),
                    idle_workers = allProcessPools.Sum(p => p.IdleWorkers),
                    healthy_pools = allProcessPools.Count(p => p.IsHealthy),
                    last_update = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };

            var jsonResult = System.Text.Json.JsonSerializer.Serialize(pluginStatus, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
                
            });
            
            _logger.LogInformation("生成的插件状态JSON长度: {JsonLength} 字符", jsonResult.Length);
            _logger.LogInformation("插件状态统计: 总插件数={TotalPlugins}, 运行中={RunningPlugins}, 空闲={IdlePlugins}, 进程池数={ProcessPoolsCount}", 
                pluginStatus.total_plugins, pluginStatus.running_plugins, pluginStatus.idle_plugins, pluginStatus.system_info.total_process_pools);
            
            return jsonResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取插件状态JSON时发生错误");
            return "{}";
        }
    }

    private string GetPythonExecutablePath(string pluginId)
    {
        // 查找虚拟环境中的Python
        var venvPath = Path.Combine(AppContext.BaseDirectory, "Envs", pluginId, ".venv");
        var pythonExe = Path.Combine(venvPath, "Scripts", "python.exe");
        
        if (File.Exists(pythonExe))
        {
            _logger.LogDebug("找到虚拟环境Python: {PythonExe}", pythonExe);
            return pythonExe;
        }

        // 回退到系统Python
        _logger.LogWarning("未找到虚拟环境Python，使用系统Python");
        return "python";
    }

}
