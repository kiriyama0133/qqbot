using InfluxDB.Client;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Options;
using qqbot.Models;

namespace qqbot.Services;

public class InfluxDbService : IDisposable
{
    private readonly ILogger<InfluxDbService> _logger;
    private readonly InfluxDBClient _client;
    private readonly InfluxDBSetting _settings;
    private readonly WriteApiAsync _writeApi;

    public InfluxDbService(IOptions<InfluxDBSetting> settings, ILogger<InfluxDbService> logger)
    {
        _logger = logger;
        _settings = settings.Value; // 返回一个 InfluxDBSetting 的实例

        // 验证配置
        if (string.IsNullOrEmpty(_settings.Url) || string.IsNullOrEmpty(_settings.Token))
        {
            throw new InvalidOperationException("InfluxDB URL 或 Token 未在 appsettings.json 中配置。");
        }

        _client = new InfluxDBClient(_settings.Url, _settings.Token);
        _writeApi = _client.GetWriteApiAsync();
        _logger.LogInformation("InfluxDb服务已经初始化，连接到{Url}", _settings.Url);
    }


    /// <summary>
    /// 异步写入一个数据点
    /// </summary>
    /// <param name="point">要写入的数据点</param>
    public async Task WritePointAsync(PointData point)
    {
        try
        {
            await _writeApi.WritePointAsync(point, _settings.Bucket, _settings.Organization);
            _logger.LogInformation("写入一条数据进入InfluxDb成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "向 InfluxDB 写入数据点时失败。");
        }
    }

    // 您未来可以在这里添加查询数据的方法
    // public async Task<YourQueryResult> QueryDataAsync(string fluxQuery) { ... }

    public void Dispose()
    {
        _client?.Dispose();
    }

}
