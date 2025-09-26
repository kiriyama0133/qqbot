using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace qqbot.Core.Services;

/// <summary>
/// 全局状态服务实现 - 管理应用的全局状态，支持响应式观察
/// </summary>
public class GlobalStateService : IGlobalStateService, IDisposable
{
    private readonly object _lock = new();
    private readonly BehaviorSubject<AppState> _stateSubject;

    public AppState CurrentState => _stateSubject.Value;
    public IObservable<AppState> StateObservable => _stateSubject.AsObservable();

    public GlobalStateService()
    {
        _stateSubject = new BehaviorSubject<AppState>(new AppState());
    }

    public void UpdateState(Func<AppState, AppState> updater)
    {
        lock (_lock)
        {
            var currentState = _stateSubject.Value;
            var newState = updater(currentState);

            if (!newState.Equals(currentState))
            {
                _stateSubject.OnNext(newState);
            }
        }
    }

    public IObservable<T> Select<T>(Func<AppState, T> selector)
    {
        return _stateSubject.AsObservable()
            .Select(selector)
            .DistinctUntilChanged();
    }

    public void Dispose()
    {
        _stateSubject.Dispose();
    }
}