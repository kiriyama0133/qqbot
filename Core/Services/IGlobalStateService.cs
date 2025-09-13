namespace qqbot.Core.Services;

public interface IGlobalStateService
{
    /// <summary>
    /// 获取当前状态的快照
    /// </summary>
    AppState CurrentState { get; }

    /// <summary>
    /// 原子性地更新状态
    /// </summary>
    /// <param name="updater">一个接收旧状态并返回新状态的函数</param>
    void UpdateState(Func<AppState, AppState> updater);

    /// <summary>
    /// 获取一个可订阅的状态流，每次状态变化都会推送新状态
    /// </summary>
    IObservable<AppState> StateObservable { get; }

    /// <summary>
    /// 选择并订阅状态的某个“部分”，只有当这部分变化时才会收到通知
    /// </summary>
    IObservable<T> Select<T>(Func<AppState, T> selector);
}