namespace qqbot.Models;

// Models/WebSocketSettings.cs
public class WebSocketSettings
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 25545;
    public string Token { get; set; } = string.Empty;
    public int HeartbeatInterval { get; set; } = 30000;
}