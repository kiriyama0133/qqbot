using System.Reactive.Linq;
using System.Reactive.Subjects;
namespace qqbot.Core.Services;

public class GlobalStateService : IGlobalStateService, IDisposable
{
    private readonly object _lock = new object();

    // BehaviorSubject 是 Rx.NET 的一个特殊主题，
    // 它会向新订阅者立即推送最新的值，并缓存最后一个值。
    private readonly BehaviorSubject<AppState> _stateSubject;

    public AppState CurrentState => _stateSubject.Value;
    public IObservable<AppState> StateObservable => _stateSubject.AsObservable();

    public GlobalStateService()
    {
        // 初始化一个默认状态
        _stateSubject = new BehaviorSubject<AppState>(new AppState());
    }

    public void UpdateState(Func<AppState, AppState> updater)
    {
        // 使用 lock 确保状态更新是线程安全的原子操作
        lock (_lock)
        {
            var currentState = _stateSubject.Value;
            var newState = updater(currentState);

            // 只有当状态真的发生变化时，才推送新状态
            if (!newState.Equals(currentState))
            {
                _stateSubject.OnNext(newState);
            }
        }
    }

    public IObservable<T> Select<T>(Func<AppState, T> selector)
    {
        // Select 用于选择状态的“部分”
        // DistinctUntilChanged 确保只有当这部分的值真的改变时，才推送通知
        return _stateSubject.AsObservable().Select(selector).DistinctUntilChanged();
    }

    public void Dispose()
    {
        _stateSubject.Dispose();
    }
}