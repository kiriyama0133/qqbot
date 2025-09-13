using Microsoft.Extensions.Options;
using qqbot.Models;
using System.Net.Http.Headers;

namespace qqbot.Helper.HttpHandlers;
/// <summary>
///  请求的认证处理，请求拦截器
/// </summary>
public class AuthHandler : DelegatingHandler
{
    private readonly HttpServiceSettings _settings;
    public AuthHandler(IOptions<HttpServiceSettings> settings)
    {
        _settings = settings.Value;
    }

    // 添加认证信息
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Console.WriteLine("添加token认证信息");
        if (!string.IsNullOrEmpty(_settings.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
