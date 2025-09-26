namespace qqbot.Handlers.HttpHandlers;

/// <summary>
/// API 异常类
/// </summary>
public class ApiException : Exception
{
    public ApiException(string message) : base(message) { }
}

/// <summary>
/// HTTP 错误和日志处理器 - 记录请求/响应日志并处理错误
/// </summary>
public class ErrorAndLoggingHandler : DelegatingHandler
{
    private readonly ILogger<ErrorAndLoggingHandler> _logger;

    public ErrorAndLoggingHandler(ILogger<ErrorAndLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("--> HTTP {Method} {Url}", request.Method, request.RequestUri);
        var response = await base.SendAsync(request, cancellationToken);

        _logger.LogInformation("<-- HTTP {StatusCode} {ReasonPhrase}", (int)response.StatusCode, response.ReasonPhrase);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("API 请求失败: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
            throw new ApiException($"API request failed with status {response.StatusCode}.");
        }

        return response;
    }
}
