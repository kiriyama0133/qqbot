using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using qqbot.Abstractions;

namespace qqbot.Services.Plugins;

/// <summary>
/// 插件服务注册器 - 负责插件发现、加载和注册
/// </summary>
public class PluginServiceRegistrar
{
    private readonly ILogger<PluginServiceRegistrar> _logger;
    private readonly string _extensionsEntryPath;

    public PluginServiceRegistrar(ILogger<PluginServiceRegistrar> logger)
    {
        _logger = logger;
        _extensionsEntryPath = Path.Combine(AppContext.BaseDirectory, "ExtensionsEntry");
        Directory.CreateDirectory(_extensionsEntryPath);
    }

    /// <summary>
    /// 发现插件（读取 plugin.json 等信息）
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
        var pluginDirectories = Directory.GetDirectories(_extensionsEntryPath);
        
        foreach (var pluginDir in pluginDirectories)
        {
            var pluginId = Path.GetFileName(pluginDir);
            var plugin = await AnalyzePluginAsync(pluginId, pluginDir);
            if (plugin != null)
            {
                plugins.Add(plugin);
                _logger.LogInformation("✅ 发现插件: {PluginId} - {Name}", pluginId, plugin.Name ?? pluginId);
            }
        }

        _logger.LogInformation("插件发现完成，共发现 {Count} 个插件", plugins.Count);
        return plugins;
    }

    /// <summary>
    /// 加载插件程序集
    /// </summary>
    public List<Assembly> LoadPluginAssemblies()
    {
        _logger.LogInformation("开始加载插件程序集...");
        var loadedAssemblies = new List<Assembly>();

        if (!Directory.Exists(_extensionsEntryPath))
        {
            _logger.LogWarning("ExtensionsEntry目录不存在: {Path}", _extensionsEntryPath);
            return loadedAssemblies;
        }

        // 确保 Abstractions 程序集已加载
        EnsureAbstractionsAssemblyLoaded();

        var pluginDirectories = Directory.GetDirectories(_extensionsEntryPath);
        
        foreach (var pluginDir in pluginDirectories)
        {
            var pluginId = Path.GetFileName(pluginDir);
            var dllFiles = Directory.GetFiles(pluginDir, "*.dll", SearchOption.TopDirectoryOnly);
            
            foreach (var dllPath in dllFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dllPath);
                    loadedAssemblies.Add(assembly);
                    _logger.LogInformation("✅ 加载插件程序集: {AssemblyName} (来自 {PluginId})", 
                        assembly.GetName().Name, pluginId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ 加载插件程序集失败: {DllPath}", Path.GetFileName(dllPath));
                }
            }
        }
        
        _logger.LogInformation("共加载 {Count} 个插件程序集", loadedAssemblies.Count);
        return loadedAssemblies;
    }

    /// <summary>
    /// 注册主程序命令处理器
    /// </summary>
    public void RegisterMainCommandHandlers(IServiceCollection services, Assembly mainAssembly)
    {
        _logger.LogInformation("开始注册主程序命令处理器...");
        
        var mainCommandHandlerTypes = mainAssembly.GetTypes()
            .Where(t => typeof(ICommandHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var handlerType in mainCommandHandlerTypes)
        {
            services.AddTransient(typeof(ICommandHandler), handlerType);
            _logger.LogInformation("已注册主程序命令处理器: {HandlerType}", handlerType.Name);
        }
    }

    /// <summary>
    /// 注册插件服务
    /// </summary>
    public List<Assembly> RegisterPluginServices(IServiceCollection services, IConfiguration configuration)
    {
        _logger.LogInformation("开始发现并注册插件服务...");
        
        var pluginAssemblies = LoadPluginAssemblies();
        
        foreach (var assembly in pluginAssemblies)
        {
            try
            {
                RegisterPluginServicesFromAssembly(services, assembly);
                RegisterInterceptorsFromAssembly(services, assembly);
                RegisterPluginModules(services, configuration, assembly);
                RegisterPluginCommandHandlers(services, assembly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "扫描插件程序集 {AssemblyName} 时发生错误", assembly.GetName().Name);
            }
        }

        return pluginAssemblies;
    }

    /// <summary>
    /// 注册 MediatR 服务
    /// </summary>
    public void RegisterMediatR(IServiceCollection services, Assembly mainAssembly, List<Assembly> pluginAssemblies)
    {
        _logger.LogInformation("注册 MediatR 服务...");
        
        EnsureAbstractionsAssemblyLoaded();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(mainAssembly);
            
            foreach (var assembly in pluginAssemblies)
            {
                RegisterMediatRHandlersFromAssembly(services, assembly);
            }
        });
    }

    /// <summary>
    /// 获取插件工作目录
    /// </summary>
    public string GetPluginWorkingDirectory(string pluginId)
    {
        return Path.Combine(_extensionsEntryPath, pluginId);
    }

    private void EnsureAbstractionsAssemblyLoaded()
    {
        try
        {
            var abstractionsAssembly = typeof(qqbot.Abstractions.BotPluginModule).Assembly;
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            if (!loadedAssemblies.Any(a => a.FullName == abstractionsAssembly.FullName))
            {
                Assembly.LoadFrom(abstractionsAssembly.Location);
                _logger.LogDebug("已预加载 Abstractions 程序集: {AssemblyName}", abstractionsAssembly.GetName().Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "预加载 Abstractions 程序集失败");
        }
    }

    private async Task<DiscoveredPlugin?> AnalyzePluginAsync(string pluginId, string pluginDirectory)
    {
        try
        {
            var dllFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);
            if (dllFiles.Length == 0)
            {
                return null;
            }

            var plugin = new DiscoveredPlugin
            {
                Id = pluginId,
                SourceDirectory = pluginDirectory,
                DllFiles = dllFiles
            };

            await LoadPluginJsonAsync(plugin, pluginDirectory);
            return plugin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分析插件失败: {PluginId}", pluginId);
            return null;
        }
    }

    private async Task LoadPluginJsonAsync(DiscoveredPlugin plugin, string pluginDirectory)
    {
        try
        {
            var pluginJsonPath = Path.Combine(pluginDirectory, "plugin.json");
            if (!File.Exists(pluginJsonPath))
            {
                return;
            }

            var jsonContent = await File.ReadAllTextAsync(pluginJsonPath);
            var pluginInfo = JsonSerializer.Deserialize<PluginJsonInfo>(jsonContent, new JsonSerializerOptions
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
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取plugin.json文件失败: {PluginId}", plugin.Id);
        }
    }

    private void RegisterMediatRHandlersFromAssembly(IServiceCollection services, Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger.LogWarning(ex, "加载插件程序集 {AssemblyName} 的部分类型失败，将尝试注册可用的 MediatR 处理器", 
                assembly.GetName().Name);
            types = ex.Types.Where(t => t != null).ToArray()!;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "加载插件程序集 {AssemblyName} 时发生错误，跳过 MediatR 注册", 
                assembly.GetName().Name);
            return;
        }

        // 注册 INotificationHandler<T> 实现
        var notificationHandlers = types
            .Where(t => t != null && !t.IsInterface && !t.IsAbstract)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && 
                           i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                .Select(i => new { HandlerType = t, NotificationType = i.GetGenericArguments()[0] }))
            .ToList();

        foreach (var handler in notificationHandlers)
        {
            try
            {
                var handlerInterface = typeof(INotificationHandler<>).MakeGenericType(handler.NotificationType);
                services.AddTransient(handlerInterface, handler.HandlerType);
                _logger.LogDebug("已注册 MediatR 通知处理器: {HandlerType} -> {NotificationType} (来自 {AssemblyName})", 
                    handler.HandlerType.Name, handler.NotificationType.Name, assembly.GetName().Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "注册 MediatR 通知处理器 {HandlerType} 时发生错误", handler.HandlerType.Name);
            }
        }

        // 注册 IRequestHandler<TRequest, TResponse> 实现
        var requestHandlers = types
            .Where(t => t != null && !t.IsInterface && !t.IsAbstract)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && 
                           i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .Select(i => new { HandlerType = t, RequestType = i.GetGenericArguments()[0], ResponseType = i.GetGenericArguments()[1] }))
            .ToList();

        foreach (var handler in requestHandlers)
        {
            try
            {
                var handlerInterface = typeof(IRequestHandler<,>).MakeGenericType(handler.RequestType, handler.ResponseType);
                services.AddTransient(handlerInterface, handler.HandlerType);
                _logger.LogDebug("已注册 MediatR 请求处理器: {HandlerType} -> {RequestType} (来自 {AssemblyName})", 
                    handler.HandlerType.Name, handler.RequestType.Name, assembly.GetName().Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "注册 MediatR 请求处理器 {HandlerType} 时发生错误", handler.HandlerType.Name);
            }
        }

        if (notificationHandlers.Any() || requestHandlers.Any())
        {
            _logger.LogInformation("已从程序集 {AssemblyName} 注册 {NotificationCount} 个通知处理器和 {RequestCount} 个请求处理器", 
                assembly.GetName().Name, notificationHandlers.Count, requestHandlers.Count);
        }
    }

    private void RegisterPluginServicesFromAssembly(IServiceCollection services, Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger.LogWarning(ex, "加载插件程序集 {AssemblyName} 的部分类型失败，将尝试加载可用的类型", 
                assembly.GetName().Name);
            types = ex.Types.Where(t => t != null).ToArray()!;
        }

        var serviceTypes = types
            .Where(t => t != null && typeof(IPluginService).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var serviceType in serviceTypes)
        {
            try
            {
                services.AddScoped(serviceType);
                _logger.LogInformation("已注册插件服务: {ServiceType} (来自 {AssemblyName})", 
                    serviceType.Name, assembly.GetName().Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "注册插件服务 {ServiceType} 时发生错误", serviceType.Name);
            }
        }
    }

    private void RegisterInterceptorsFromAssembly(IServiceCollection services, Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).ToArray()!;
        }

        var interceptorTypes = types
            .Where(t => t != null && t.IsSubclassOf(typeof(DelegatingHandler)) && 
                       t.GetCustomAttribute<HttpClientInterceptorAttribute>() != null);

        foreach (var interceptorType in interceptorTypes)
        {
            var attribute = interceptorType.GetCustomAttribute<HttpClientInterceptorAttribute>();
            if (attribute == null)
            {
                continue;
            }

            try
            {
                services.AddTransient(interceptorType);
                string clientName = attribute.Target.ToString();
                
                var addHandlerMethod = typeof(HttpClientBuilderExtensions)
                    .GetMethod(nameof(AddHttpMessageHandlerHelper), BindingFlags.NonPublic | BindingFlags.Static)
                    ?.MakeGenericMethod(interceptorType);
                
                if (addHandlerMethod != null)
                {
                    addHandlerMethod.Invoke(null, new object[] { services, clientName });
                    _logger.LogInformation("已注册HTTP拦截器: {InterceptorType} -> {ClientName}", 
                        interceptorType.Name, clientName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "注册HTTP拦截器 {InterceptorType} 时发生错误", interceptorType.Name);
            }
        }
    }

    private void RegisterPluginModules(IServiceCollection services, IConfiguration configuration, Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger.LogWarning(ex, "加载插件程序集 {AssemblyName} 的部分类型失败，将尝试加载可用的类型", 
                assembly.GetName().Name);
            types = ex.Types.Where(t => t != null).ToArray()!;
        }

        var pluginModuleTypes = types
            .Where(t => t != null && typeof(BotPluginModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var moduleType in pluginModuleTypes)
        {
            try
            {
                var moduleInstance = Activator.CreateInstance(moduleType, 
                    new object[] { null, null }) as BotPluginModule;

                if (moduleInstance != null)
                {
                    moduleInstance.ConfigureServices(services, configuration);
                    _logger.LogInformation("已配置插件服务: {ModuleType} (来自 {AssemblyName})", 
                        moduleType.Name, assembly.GetName().Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "配置插件服务 {ModuleType} 时发生错误", moduleType.Name);
            }
        }
    }

    private void RegisterPluginCommandHandlers(IServiceCollection services, Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).ToArray()!;
        }

        var pluginHandlerTypes = types
            .Where(t => t != null && typeof(ICommandHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var handlerType in pluginHandlerTypes)
        {
            services.AddTransient(typeof(ICommandHandler), handlerType);
            _logger.LogInformation("已注册插件命令处理器: {HandlerType} (来自 {AssemblyName})", 
                handlerType.Name, assembly.GetName().Name);
        }
    }

    private static void AddHttpMessageHandlerHelper<T>(IServiceCollection services, string clientName) 
        where T : DelegatingHandler
    {
        services.AddHttpClient(clientName).AddHttpMessageHandler<T>();
    }
}

/// <summary>
/// 发现的插件信息
/// </summary>
public class DiscoveredPlugin
{
    public string Id { get; set; } = string.Empty;
    public string SourceDirectory { get; set; } = string.Empty;
    public string[] DllFiles { get; set; } = Array.Empty<string>();
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public string[] Dependencies { get; set; } = Array.Empty<string>();
    public string? PluginJsonFile { get; set; }
}

/// <summary>
/// plugin.json 文件的信息结构
/// </summary>
public class PluginJsonInfo
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public List<string>? Dependencies { get; set; }
}