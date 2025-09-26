namespace qqbot.Core.Services;

/// <summary>
/// 动态状态服务接口 - 提供基于键值对的状态存储和观察
/// </summary>
public interface IDynamicStateService
{
    /// <summary>
    /// 设置状态值（如果键不存在则创建）
    /// </summary>
    void SetState(string key, object value);

    /// <summary>
    /// 获取状态值，不存在时返回默认值
    /// </summary>
    T? GetState<T>(string key, T? defaultValue = default);

    /// <summary>
    /// 获取状态的可观察流，状态变化时自动推送
    /// </summary>
    IObservable<T> GetStateObservable<T>(string key);
}