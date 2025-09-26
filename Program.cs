using Extensions;
using Microsoft.EntityFrameworkCore;
using qqbot.Core.Services;
using qqbot.Data;
using qqbot.Handlers.HttpHandlers;
using qqbot.Models.Config;
using qqbot.Services;
using qqbot.Services.Images;
using qqbot.Services.Plugins;
using System.Threading.Tasks;

namespace qqbot;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.AddServiceDefaults();
        builder.AddNpgsqlDbContext<MessageDbContext>("botDb");
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddMemoryCache();
        services.AddSingleton<IGlobalStateService, GlobalStateService>(); // 添加全局状态服务
        services.AddSingleton<IDynamicStateService, DynamicStateService>(); // 添加动态状态服务
        services.AddSingleton<StateMonitorService>(); // 添加状态监控服务
        services.AddTransient<FileCacheHttpService>(); // 添加文件缓存服务
        services.AddSingleton<ImageDownloadCache>(); // 添加图片下载缓存服务
        services.AddTransient<ImageCacheHelper>(); // 添加图片缓存辅助服务
        services.AddHostedService<StateMonitorService>(provider => provider.GetRequiredService<StateMonitorService>()); // 添加状态监控服务作为后台服务
        
        // 注册插件管理服务
        services.AddSingleton<PluginStateManager>();
        services.AddSingleton<PluginServiceRegistrar>();

        // 注册插件和命令处理器
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var pluginRegistrar = new PluginServiceRegistrar(
            loggerFactory.CreateLogger<PluginServiceRegistrar>());
        var mainAssembly = typeof(Program).Assembly;
        
        pluginRegistrar.RegisterMainCommandHandlers(services, mainAssembly);
        var pluginAssemblies = pluginRegistrar.RegisterPluginServices(services, configuration);
        pluginRegistrar.RegisterMediatR(services, mainAssembly, pluginAssemblies);

        services.Configure<WebSocketSettings>(configuration.GetSection("WebSocketClientSettings"));
        services.Configure<HttpServiceSettings>(configuration.GetSection("HttpServiceSettings"));
        
        // 验证配置，确保所有必需配置都存在
        ValidateConfiguration(configuration);
        services.AddHostedService<EventWebSocketClient>();
        services.AddHostedService<CommandRegistry>(); 
        services.AddScoped<MessageDbContext>();
        services.AddSingleton<CommandRegistry>(); // CommandRegistry 现在可以被安全地创建
        services.AddTransient<FileCacheHttpService>(); // 下载文件的服务

        // 注册 HttpClient 管道
        services.AddTransient<AuthHandler>();
        services.AddTransient<ErrorAndLoggingHandler>();
        var httpClientBuilder = services.AddHttpClient<NapCatApiService>()
            .AddHttpMessageHandler<ErrorAndLoggingHandler>()
            .AddHttpMessageHandler<AuthHandler>();

        var app = builder.Build();
        Console.WriteLine("开始初始化插件系统...");
        var pluginStateManager = app.Services.GetRequiredService<PluginStateManager>();
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

        await MigrationToPostgres(app);

        // 运行应用
        app.Run();
    }

    public static async Task MigrationToPostgres(IHost app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<MessageDbContext>();
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred during migration");
        }
    }
    
    /// <summary>
    /// 验证配置，确保所有必需配置都存在
    /// </summary>
    private static void ValidateConfiguration(IConfiguration configuration)
    {
        // 验证 WebSocketSettings
        var wsSection = configuration.RequireSection("WebSocketClientSettings");
        wsSection.RequireProperty("Host", "WebSocketClientSettings");
        wsSection.RequireNumericProperty("Port", "WebSocketClientSettings");
        wsSection.RequireProperty("Token", "WebSocketClientSettings");
        wsSection.RequireNumericProperty("HeartbeatInterval", "WebSocketClientSettings");
        
        // 验证 HttpServiceSettings
        var httpSection = configuration.RequireSection("HttpServiceSettings");
        httpSection.RequireProperty("Host", "HttpServiceSettings");
        httpSection.RequireNumericProperty("Port", "HttpServiceSettings");
        httpSection.RequireProperty("Token", "HttpServiceSettings");
    }
}