using Microsoft.Extensions.DependencyInjection;
using qqbot.Abstractions;
using qqbot.Helper;
using qqbot.Helper.HttpHandlers;
using qqbot.Models;
using qqbot.Services;
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

            var pluginAssemblies = PluginLoaderExtensions.DiscoverPluginAssemblies();
            var allAssemblies = new List<Assembly> { typeof(Program).Assembly };
            allAssemblies.AddRange(pluginAssemblies);
            services.AddMemoryCache(); // 添加内存缓存
            services.AddSingleton<IGlobalStateService, GlobalStateService>(); // 添加全局状态服务
            services.AddSingleton<IDynamicStateService, DynamicStateService>(); // 添加动态状态服务

            // 统一注册所有 Handlers (包括主程序和所有插件的)
            Console.WriteLine("开始注册命令处理器 (Command Handlers)...");
            var commandHandlerTypes = allAssemblies.SelectMany(a => a.GetTypes())
                .Where(t => typeof(ICommandHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var handlerType in commandHandlerTypes)
            {
                services.AddTransient(typeof(ICommandHandler), handlerType);
                Console.WriteLine($"  -> 已注册命令处理器: {handlerType.Name}");
            }

            // 注册 MediatR，让它也扫描所有程序集来找到 INotificationHandler
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(allAssemblies.ToArray()));

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

            // 在应用构建完成后，DI 容器完全可用，此时再初始化命令注册处
            var commandRegistry = app.Services.GetRequiredService<CommandRegistry>();


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