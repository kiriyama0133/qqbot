namespace qqbot.Core.Services;

/// <summary>
/// 全局状态服务接口 - 管理应用的全局状态（AppState）
/// </summary>
public interface IGlobalStateService
{
    /// <summary>
    /// 当前状态快照
    /// </summary>
    AppState CurrentState { get; }

    /// <summary>
    /// 原子性更新状态
    /// </summary>
    /// <param name="updater">状态更新函数：接收旧状态，返回新状态</param>
    void UpdateState(Func<AppState, AppState> updater);

    /// <summary>
    /// 状态变化可观察流
    /// </summary>
    IObservable<AppState> StateObservable { get; }

    /// <summary>
    /// 选择状态的一部分进行观察，仅在该部分变化时推送
    /// </summary>
    /// <typeparam name="T">选择的状态类型</typeparam>
    /// <param name="selector">状态选择器函数</param>
    IObservable<T> Select<T>(Func<AppState, T> selector);
}