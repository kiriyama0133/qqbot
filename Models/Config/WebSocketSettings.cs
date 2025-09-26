namespace qqbot.Models.Config;

public class WebSocketSettings
{
    public required string Host { get; set; }
    public required int Port { get; set; }
    public required string Token { get; set; }
    public required int HeartbeatInterval { get; set; }
}
