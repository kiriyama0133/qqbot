using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace qqbot.Abstractions;

/// <summary>
/// 所有插件模块都必须继承的抽象基类
/// </summary>
public abstract class BotPluginModule : ICommandHandler
{
    public abstract CommandDefinition Command { get; }

    /// <summary>
    /// 插件通过此方法将其服务注册到主应用程序的DI容器中
    /// </summary>
    public abstract void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// (可选) 插件通过此方法向 NapCatApiService 的 HttpClient 管道中添加自定义的拦截器
    /// </summary>
    /// <returns>DelegatingHandler 的类型列表</returns>
    public virtual IEnumerable<Type> GetHttpMessageHandlers()
    {
        // 默认返回一个空列表
        return Enumerable.Empty<Type>();
    }
}
