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

namespace qqbot
{
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
            
            // 注册Python插件管理服务
            Console.WriteLine("注册Python插件管理服务...");
            services.AddSingleton<PluginDiscoveryService>(); // 插件发现服务
            services.AddSingleton<PythonEnvManager>(); // Python环境管理器
            services.AddSingleton<PythonProcessManager>(); // Python进程管理器
            Console.WriteLine("  -> Python插件管理服务注册完成");

            // 先只注册主程序的Handlers，插件Handlers将在文件复制完成后注册
            Console.WriteLine("开始注册主程序命令处理器...");
            var mainAssembly = typeof(Program).Assembly;
            var mainCommandHandlerTypes = mainAssembly.GetTypes()
                .Where(t => typeof(ICommandHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var handlerType in mainCommandHandlerTypes)
            {
                services.AddTransient(typeof(ICommandHandler), handlerType);
                Console.WriteLine($"  -> 已注册主程序命令处理器: {handlerType.Name}");
            }

            // 先注册主程序的MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(mainAssembly));

            // 注册主程序的核心服务和配置
            services.Configure<WebSocketSettings>(configuration.GetSection("WebSocketClientSettings"));
            services.Configure<HttpServiceSettings>(configuration.GetSection("HttpServiceSettings"));
            services.AddHostedService<EventWebSocketClient>();
            services.AddHostedService<CommandRegistry>(); // 
            services.AddSingleton<CommandRegistry>(); // CommandRegistry 现在可以被安全地创建

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
            var pluginDiscovery = app.Services.GetRequiredService<PluginDiscoveryService>();
            var pythonEnvManager = app.Services.GetRequiredService<PythonEnvManager>();
            var pythonProcessManager = app.Services.GetRequiredService<PythonProcessManager>();
            
            // 异步初始化插件系统
            _ = Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine("发现和复制插件文件...");
                    var discoveredPlugins = await pluginDiscovery.DiscoverPluginsAsync();
                    
                    Console.WriteLine("发现插件程序集...");
                    var pluginAssemblies = PluginLoaderExtensions.DiscoverPluginAssemblies();
                    var allAssemblies = new List<Assembly> { typeof(Program).Assembly };
                    allAssemblies.AddRange(pluginAssemblies);
                    
                    Console.WriteLine("发现插件命令处理器...");
                    var pluginCommandHandlerTypes = pluginAssemblies.SelectMany(a => a.GetTypes())
                        .Where(t => typeof(ICommandHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    Console.WriteLine($"  -> 发现 {pluginCommandHandlerTypes.Count()} 个插件命令处理器:");
                    foreach (var handlerType in pluginCommandHandlerTypes)
                    {
                        Console.WriteLine($"    - {handlerType.Name}");
                    }
                    
                    Console.WriteLine("插件程序集发现完成");
                    Console.WriteLine($"  -> 发现 {pluginAssemblies.Count} 个插件程序集");
                    Console.WriteLine("  -> 注意：插件命令处理器需要在应用启动时重新注册才能生效");
                    
                    if (discoveredPlugins.Count == 0)
                    {
                        Console.WriteLine("未发现任何插件");
                        return;
                    }
                    
                    Console.WriteLine($"初始化 {discoveredPlugins.Count} 个插件:");
                    foreach (var plugin in discoveredPlugins)
                    {
                        Console.WriteLine($"  - {plugin.Id} (类型: {plugin.Type})");
                        
                        // 为Python插件设置环境
                        if (plugin.Type == PluginType.Python || plugin.Type == PluginType.Hybrid)
                        {
                            try
                            {
                                var envDirectory = Path.Combine(AppContext.BaseDirectory, "Envs", plugin.Id);
                                Directory.CreateDirectory(envDirectory);
                                
                                var toolInfo = new PythonToolInfo
                                {
                                    Script = Path.GetFileName(plugin.MainScript ?? "main.py"),
                                    Requirements = plugin.RequirementsFile ?? Path.Combine(plugin.SourceDirectory, "requirements.txt")
                                };
                                
                                var pythonExePath = pythonEnvManager.SetupEnvironmentForPlugin(envDirectory, toolInfo);
                                Console.WriteLine($"  ✅ {plugin.Id} 环境设置完成: {pythonExePath}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"  ❌ {plugin.Id} 环境设置失败: {ex.Message}");
                            }
                        }
                    }
                    
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
}