using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using qqbot.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
namespace qqbot.Services;

/// <summary>
/// 负责与 NapCat HTTP API 进行通信的服务
/// </summary>
public partial class NapCatApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NapCatApiService> _logger;

    public NapCatApiService(
        HttpClient httpClient,
        ILogger<NapCatApiService> logger,
        IOptions<HttpServiceSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;

        string baseUrl = String.Concat("http://",settings.Value.Host, ":", settings.Value.Port.ToString());

        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new InvalidOperationException("baseurl配置错误，检查appsettings");
        }

        _httpClient.BaseAddress = new Uri(baseUrl);
    }
}