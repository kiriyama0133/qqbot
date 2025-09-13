namespace qqbot.Helper.HttpHandlers;
public class ApiException : Exception
{
    public ApiException(string message) : base(message) { }
}

public class ErrorAndLoggingHandler : DelegatingHandler
{
    private readonly ILogger<ErrorAndLoggingHandler> _logger;
    public ErrorAndLoggingHandler(ILogger<ErrorAndLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 请求拦截器部分-日志
        _logger.LogInformation("--> HTTP {Method} {Url}", request.Method, request.RequestUri);
        var response = await base.SendAsync(request, cancellationToken);

        // --- 响应拦截部分（日志和错误处理） ---
        _logger.LogInformation("<-- HTTP {StatusCode} {ReasonPhrase}", (int)response.StatusCode, response.ReasonPhrase);

        // 如果响应状态码表示失败
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("API 请求失败: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
            // 抛出一个自定义异常，这样业务代码就不用每次都检查状态码了
            throw new ApiException($"API request failed with status {response.StatusCode}.");
        }

        return response;
    }
}
