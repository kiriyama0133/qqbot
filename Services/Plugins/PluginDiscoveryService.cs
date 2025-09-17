using System.Diagnostics;
using System.Text.Json;

namespace qqbot.Services.Plugins;

/// <summary>
/// 插件发现服务，负责扫描ExtensionsEntry目录并管理插件
/// </summary>
public class PluginDiscoveryService
{
    private readonly ILogger<PluginDiscoveryService> _logger;
    private readonly string _extensionsEntryPath;

    public PluginDiscoveryService(ILogger<PluginDiscoveryService> logger)
    {
        _logger = logger;
        _extensionsEntryPath = Path.Combine(AppContext.BaseDirectory, "ExtensionsEntry");
        
        // 确保目录存在
        Directory.CreateDirectory(_extensionsEntryPath);
        
        _logger.LogInformation("插件发现服务已初始化，扫描目录: {ExtensionsEntryPath}", _extensionsEntryPath);
    }

    /// <summary>
    /// 发现所有插件
    /// </summary>
    public async Task<List<DiscoveredPlugin>> DiscoverPluginsAsync()
    {
        var plugins = new List<DiscoveredPlugin>();
        
        if (!Directory.Exists(_extensionsEntryPath))
        {
            _logger.LogWarning("ExtensionsEntry目录不存在: {Path}", _extensionsEntryPath);
            return plugins;
        }

        _logger.LogInformation("开始扫描ExtensionsEntry目录: {Path}", _extensionsEntryPath);

        // 扫描所有子目录
        var pluginDirectories = Directory.GetDirectories(_extensionsEntryPath);
        
        foreach (var pluginDir in pluginDirectories)
        {
            var pluginId = Path.GetFileName(pluginDir);
            _logger.LogInformation("发现插件目录: {PluginId}", pluginId);

            var plugin = await AnalyzePluginAsync(pluginId, pluginDir);
            if (plugin != null)
            {
                plugins.Add(plugin);
                _logger.LogInformation("✅ 插件分析完成: {PluginId} (类型: {Type})", pluginId, plugin.Type);
            }
        }

        _logger.LogInformation("插件发现完成，共发现 {Count} 个插件", plugins.Count);
        return plugins;
    }

    /// <summary>
    /// 分析单个插件
    /// </summary>
    private async Task<DiscoveredPlugin?> AnalyzePluginAsync(string pluginId, string pluginDirectory)
    {
        try
        {
            var plugin = new DiscoveredPlugin
            {
                Id = pluginId,
                SourceDirectory = pluginDirectory,
                Type = PluginType.Unknown
            };

            // 检查是否为.NET插件（包含.dll文件）
            var dllFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);
            if (dllFiles.Length > 0)
            {
                plugin.Type = PluginType.DotNet;
                plugin.DllFiles = dllFiles;
            }

            // 检查是否为Python插件（包含.py文件）
            var pyFiles = Directory.GetFiles(pluginDirectory, "*.py", SearchOption.AllDirectories);
            if (pyFiles.Length > 0)
            {
                if (plugin.Type == PluginType.DotNet)
                {
                    plugin.Type = PluginType.Hybrid; // 混合插件
                }
                else
                {
                    plugin.Type = PluginType.Python;
                }
                
                plugin.PythonFiles = pyFiles;
                plugin.MainScript = FindMainScript(pyFiles);
                plugin.RequirementsFile = FindRequirementsFile(pluginDirectory);
                plugin.PyProjectFile = FindPyProjectFile(pluginDirectory);
            }

            // 检查是否为纯配置插件
            if (plugin.Type == PluginType.Unknown)
            {
                var configFiles = Directory.GetFiles(pluginDirectory, "*.json", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(pluginDirectory, "*.yaml", SearchOption.AllDirectories))
                    .Concat(Directory.GetFiles(pluginDirectory, "*.yml", SearchOption.AllDirectories));
                
                if (configFiles.Any())
                {
                    plugin.Type = PluginType.Configuration;
                    plugin.ConfigFiles = configFiles.ToArray();
                }
            }

            // 读取plugin.json文件（如果存在）
            await LoadPluginJsonAsync(plugin, pluginDirectory);

            return plugin.Type != PluginType.Unknown ? plugin : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分析插件失败: {PluginId}", pluginId);
            return null;
        }
    }


    /// <summary>
    /// 查找主脚本文件
    /// </summary>
    private string? FindMainScript(string[] pyFiles)
    {
        // 优先查找main.py
        var mainPy = pyFiles.FirstOrDefault(f => Path.GetFileName(f).Equals("main.py", StringComparison.OrdinalIgnoreCase));
        if (mainPy != null) return mainPy;

        // 查找与插件同名的文件
        var pluginName = Path.GetFileName(Path.GetDirectoryName(pyFiles[0]));
        var pluginPy = pyFiles.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(pluginName, StringComparison.OrdinalIgnoreCase));
        if (pluginPy != null) return pluginPy;

        // 返回第一个Python文件
        return pyFiles[0];
    }

    /// <summary>
    /// 查找依赖文件
    /// </summary>
    private string? FindRequirementsFile(string pluginDirectory)
    {
        var requirementsPath = Path.Combine(pluginDirectory, "requirements.txt");
        return File.Exists(requirementsPath) ? requirementsPath : null;
    }

    /// <summary>
    /// 查找pyproject.toml文件
    /// </summary>
    private string? FindPyProjectFile(string pluginDirectory)
    {
        var pyProjectPath = Path.Combine(pluginDirectory, "pyproject.toml");
        return File.Exists(pyProjectPath) ? pyProjectPath : null;
    }


    /// <summary>
    /// 读取plugin.json文件并填充插件信息
    /// </summary>
    private async Task LoadPluginJsonAsync(DiscoveredPlugin plugin, string pluginDirectory)
    {
        try
        {
            var pluginJsonPath = Path.Combine(pluginDirectory, "plugin.json");
            if (File.Exists(pluginJsonPath))
            {
                var jsonContent = await File.ReadAllTextAsync(pluginJsonPath);
                var pluginInfo = System.Text.Json.JsonSerializer.Deserialize<PluginJsonInfo>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (pluginInfo != null)
                {
                    plugin.PluginJsonFile = pluginJsonPath;
                    plugin.Name = pluginInfo.Name ?? plugin.Id;
                    plugin.Version = pluginInfo.Version;
                    plugin.Description = pluginInfo.Description;
                    plugin.Author = pluginInfo.Author;
                    plugin.Dependencies = pluginInfo.Dependencies?.ToArray() ?? Array.Empty<string>();
                    
                    _logger.LogDebug("已读取插件信息: {PluginId} - {Name} v{Version}", 
                        plugin.Id, plugin.Name, plugin.Version);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取plugin.json文件失败: {PluginId}", plugin.Id);
        }
    }

    /// <summary>
    /// 获取插件的工作目录（用于Python环境）
    /// </summary>
    public string GetPluginWorkingDirectory(string pluginId)
    {
        return Path.Combine(_extensionsEntryPath, pluginId);
    }
}

/// <summary>
/// 发现的插件信息
/// </summary>
public class DiscoveredPlugin
{
    public string Id { get; set; } = string.Empty;
    public string SourceDirectory { get; set; } = string.Empty;
    public PluginType Type { get; set; }
    public string[] DllFiles { get; set; } = Array.Empty<string>();
    public string[] PythonFiles { get; set; } = Array.Empty<string>();
    public string[] ConfigFiles { get; set; } = Array.Empty<string>();
    public string? MainScript { get; set; }
    public string? RequirementsFile { get; set; }
    public string? PyProjectFile { get; set; }
    
    // 从plugin.json读取的属性
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public string[] Dependencies { get; set; } = Array.Empty<string>();
    public string? PluginJsonFile { get; set; }
}

/// <summary>
/// 插件类型
/// </summary>
public enum PluginType
{
    Unknown,
    DotNet,      // .NET插件
    Python,      // Python插件
    Hybrid,      // 混合插件（同时包含.NET和Python）
    Configuration // 配置插件
}

/// <summary>
/// plugin.json文件的信息结构
/// </summary>
public class PluginJsonInfo
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? MainScript { get; set; }
    public string? Requirements { get; set; }
    public string? Author { get; set; }
    public List<string>? Dependencies { get; set; }
}
