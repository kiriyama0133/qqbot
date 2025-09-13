using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using qqbot.Helper;
using qqbot.Models;
using qqbot.Models.Notifications;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static qqbot.Models.Group;
namespace qqbot.Services;

public class EventWebSocketClient : IHostedService,IDisposable
{
    private readonly ILogger<EventWebSocketClient> _logger;
    private readonly WebSocketSettings _settings;
    private ClientWebSocket _client;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly IServiceProvider _serviceProvider;

    public EventWebSocketClient(IOptions<WebSocketSettings> settings, ILogger<EventWebSocketClient> logger, IServiceProvider serviceProvider)
    {
        _settings = settings.Value;
        _logger = logger;
        _client = new ClientWebSocket();
        _cancellationTokenSource = new CancellationTokenSource();
        _serviceProvider = serviceProvider; 
    }

    public Task StartAsync(CancellationToken cancellationToken) { 
        _logger.LogInformation("EventWebSocketClient StartAsync");
        Task.Run(() => ListenLoopAsync(_cancellationTokenSource.Token));
        return Task.CompletedTask;
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("EventWebSocketClient ListenLoopAsync");
                if(_client.State != WebSocketState.None)
                {
                    _client.Dispose();
                    _client = new ClientWebSocket();
                }

                // 设置token
                _client.Options.SetRequestHeader("Authorization", $"Bearer {_settings.Token}");
                var uri = new Uri($"ws://{_settings.Host}:{_settings.Port}");
                await _client.ConnectAsync(uri, cancellationToken);
                _logger.LogInformation("WebSocket 连接成功！正在等待消息...");

                // 设置缓冲区
                var buffer = new byte[1024 * 4];
                using var ms = new MemoryStream();
                while (_client.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;
                    ms.SetLength(0); // 重置内存流
                    do
                    {
                        result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        ms.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage); // 循环接收数据

                    if (result.MessageType == WebSocketMessageType.Close) { break; }
                    ms.Seek(0, SeekOrigin.Begin);
                    var message = Encoding.UTF8.GetString(ms.ToArray());
                    await ProcessAndPublishEvent(message); // 处理并发布事件
                    }
                }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket 连接出现错误。");
            }
            _logger.LogInformation("连接已断开，将在5秒后尝试重连...");
            await Task.Delay(5000, cancellationToken);
        }
    }

    private async Task ProcessAndPublishEvent(string messageJson)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(messageJson);
            var eventData = jsonDoc.RootElement;

            if (!eventData.TryGetProperty("post_type", out var postTypeProp)) return;

            string postType = postTypeProp.GetString() ?? "unknown";

            // 创建一个新的 DI 作用域
            using (var scope = _serviceProvider.CreateScope())
            {
                // 从当前作用域中获取 IMediator 实例
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                // 根据事件类型，发布不同的通知
                if (postType == "message")
                {
                    if (eventData.TryGetProperty("message_type", out var messageTypeProp))
                    {
                        string messageType = messageTypeProp.GetString() ?? "";
                        if (messageType == "private") // 私聊消息的订阅发布
                        {
                            var privateMessage = eventData.Deserialize<PrivateMessageEvent>();
                            if (privateMessage != null)
                                await mediator.Publish(new PrivateMessageReceivedNotification(privateMessage));
                        }
                        else if (messageType == "group") // 群消息
                        {
                            var groupMessage = eventData.Deserialize<GroupMessageEvent>();
                            if (groupMessage != null)
                                await mediator.Publish(new GroupMessageReceivedNotification(groupMessage));
                        }
                    }
                }
                // else if (postType == "notice") { ... }
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON 解析失败。收到的原始文本: {RawText}", messageJson);
        }
    }

    // 停止服务
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Event WebSocket Client is stopping.");
        _cancellationTokenSource.Cancel();
        if (_client.State == WebSocketState.Open)
        {
            await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Service stopping", cancellationToken);
        }
    }

    // 释放资源
    public void Dispose()
    {
        _client.Dispose();
        _cancellationTokenSource.Dispose();
    }

}
