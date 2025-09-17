using Microsoft.Extensions.DependencyInjection;
using qqbot.Abstractions;
using qqbot.Helper;
using qqbot.Helper.HttpHandlers;
using qqbot.Models;
using qqbot.Services;
using qqbot.Services.Plugins; // 添加Python插件服务
using System.Reflection;
using System.Net.Http;
using qqbot.Core.Services; // 确保 using
using qqbot.RedisCache;
namespace qqbot;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddMemoryCache(); // 添加内存缓存
        services.AddSingleton<IGlobalStateService, GlobalStateService>(); // 添加全局状态服务
        services.AddSingleton<IDynamicStateService, DynamicStateService>(); // 添加动态状态服务
        services.AddSingleton<StateMonitorService>(); // 添加状态监控服务
        services.AddHostedService<StateMonitorService>(provider => provider.GetRequiredService<StateMonitorService>()); // 添加状态监控服务作为后台服务
        
        // 注册Python插件管理服务
        Console.WriteLine("注册Python插件管理服务...");
        services.AddSingleton<PluginDiscoveryService>(); // 插件发现服务
        services.AddSingleton<PythonEnvManager>(); // Python环境管理器
        services.AddSingleton<PythonProcessManager>(); // Python进程管理器
        services.AddSingleton<PluginStateManager>(); // 插件状态管理器
        Console.WriteLine("  -> Python插件管理服务注册完成");

        // 注册主程序和插件的Handlers
        Console.WriteLine("开始注册命令处理器...");
        var mainAssembly = typeof(Program).Assembly;
        
        // 注册主程序的命令处理器
        var mainCommandHandlerTypes = mainAssembly.GetTypes()
            .Where(t => typeof(ICommandHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var handlerType in mainCommandHandlerTypes)
        {
            services.AddTransient(typeof(ICommandHandler), handlerType);
            Console.WriteLine($"  -> 已注册主程序命令处理器: {handlerType.Name}");
        }

        // 发现并注册插件程序集中的命令处理器
        Console.WriteLine("发现并注册插件命令处理器...");
        var pluginAssemblies = PluginLoaderExtensions.DiscoverPluginAssemblies();
        foreach (var assembly in pluginAssemblies)
        {
            try
            {
                // 注册插件中的服务类型
                var serviceTypes = assembly.GetTypes()
                    .Where(t => typeof(IPluginService).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var serviceType in serviceTypes)
                {
                    try
                    {
                        services.AddScoped(serviceType);
                        Console.WriteLine($"  -> 已注册插件服务: {serviceType.Name} (来自 {assembly.GetName().Name})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  -> 注册插件服务 {serviceType.Name} 时发生错误: {ex.Message}");
                    }
                }

                // 调用插件的 ConfigureServices 方法
                var pluginModuleTypes = assembly.GetTypes()
                    .Where(t => typeof(BotPluginModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var moduleType in pluginModuleTypes)
                {
                    try
                    {
                        // 创建插件模块实例并调用 ConfigureServices
                        var moduleInstance = Activator.CreateInstance(moduleType, 
                            new object[] { null, null }) as BotPluginModule; // 传入 null 参数，因为我们使用服务定位器
                        
                        if (moduleInstance != null)
                        {
                            moduleInstance.ConfigureServices(services, configuration);
                            Console.WriteLine($"  -> 已配置插件服务: {moduleType.Name} (来自 {assembly.GetName().Name})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  -> 配置插件服务 {moduleType.Name} 时发生错误: {ex.Message}");
                    }
                }

                // 然后注册命令处理器
                var pluginHandlerTypes = assembly.GetTypes()
                    .Where(t => typeof(ICommandHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var handlerType in pluginHandlerTypes)
                {
                    services.AddTransient(typeof(ICommandHandler), handlerType);
                    Console.WriteLine($"  -> 已注册插件命令处理器: {handlerType.Name} (来自 {assembly.GetName().Name})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  -> 扫描插件程序集 {assembly.GetName().Name} 时发生错误: {ex.Message}");
            }
        }

        // 注册主程序和插件的MediatR
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(mainAssembly);
            foreach (var assembly in pluginAssemblies)
            {
                cfg.RegisterServicesFromAssembly(assembly);
            }
        });

        // 注册主程序的核心服务和配置
        services.Configure<WebSocketSettings>(configuration.GetSection("WebSocketClientSettings"));
        services.Configure<InfluxDBSetting>(configuration.GetSection("InfluxDB"));
        services.Configure<HttpServiceSettings>(configuration.GetSection("HttpServiceSettings"));
        services.Configure<RedisSetting>(configuration.GetSection("RedisSetting"));
        services.AddHostedService<EventWebSocketClient>();
        services.AddHostedService<CommandRegistry>(); // 
        services.AddSingleton<CommandRegistry>(); // CommandRegistry 现在可以被安全地创建
        services.AddSingleton<InfluxDbService>(); // influx数据库
        services.AddSingleton<RedisService>(); // redis服务

        // 注册 HttpClient 管道
        services.AddTransient<AuthHandler>();
        services.AddTransient<ErrorAndLoggingHandler>();
        var httpClientBuilder = services.AddHttpClient<NapCatApiService>()
            .AddHttpMessageHandler<ErrorAndLoggingHandler>()
            .AddHttpMessageHandler<AuthHandler>();

        // 注册 ASP.NET Core 框架服务
        services.AddControllers();

        var app = builder.Build();

        // 在应用构建完成后，DI 容器完全可用，此时再初始化插件系统
        Console.WriteLine("开始初始化插件系统...");
        var pluginStateManager = app.Services.GetRequiredService<PluginStateManager>();
        
        // 异步初始化插件系统
        _ = Task.Run(async () =>
        {
            try
            {
                await pluginStateManager.InitializeAsync();
                Console.WriteLine("✅ 插件系统初始化完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 插件系统初始化失败: {ex.Message}");
            }
        });


        // 配置 HTTP 请求管道
        if (app.Environment.IsDevelopment())
        {
            // 可选，添加 Swagger 等开发工具
        }
        app.UseRouting();
        app.MapControllers();

        // 运行应用
        app.Run();
    }
}