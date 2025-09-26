using Microsoft.Extensions.Options;
using qqbot.Models.Config;
using System.Net.Http.Headers;

namespace qqbot.Handlers.HttpHandlers;

/// <summary>
/// HTTP 认证处理器 - 为请求添加认证信息
/// </summary>
public class AuthHandler : DelegatingHandler
{
    private readonly HttpServiceSettings _settings;

    public AuthHandler(IOptions<HttpServiceSettings> settings)
    {
        _settings = settings.Value;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_settings.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
