namespace qqbot.Core;

/// <summary>
/// 定义应用的全局状态。
/// 使用 record 类型，鼓励不可变性（Immutability）。
/// </summary>
public record AppState
{
    public string BotNickname { get; init; } = "MyBot";
    public long SelfId { get; init; }
    public bool IsMuted { get; init; } = false;
    public int ReceivedMessageCount { get; init; } = 0;
    public long LastReceivedGroupId { get; init; }
    // ... 未来可以添加更多全局状态
}