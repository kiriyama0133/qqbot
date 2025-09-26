using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace qqbot.Core.Services;

/// <summary>
/// 动态状态服务实现 - 基于键值对的状态管理，支持响应式观察
/// </summary>
public class DynamicStateService : IDynamicStateService, IDisposable
{
    private readonly ConcurrentDictionary<string, ISubject<object>> _stateSubjects = new();

    public void SetState(string key, object value)
    {
        var subject = _stateSubjects.GetOrAdd(key, _ => new BehaviorSubject<object>(value));
        subject.OnNext(value);
    }

    public T? GetState<T>(string key, T? defaultValue = default)
    {
        if (_stateSubjects.TryGetValue(key, out var subject) && 
            subject is BehaviorSubject<object> behaviorSubject &&
            behaviorSubject.TryGetValue(out var value))
        {
            return value is T typedValue ? typedValue : defaultValue;
        }
        return defaultValue;
    }

    public IObservable<T> GetStateObservable<T>(string key)
    {
        var subject = _stateSubjects.GetOrAdd(key, _ => new BehaviorSubject<object>(default(T)!));
        return subject.AsObservable().OfType<T>();
    }

    public void Dispose()
    {
        foreach (var subject in _stateSubjects.Values)
        {
            (subject as IDisposable)?.Dispose();
        }
        _stateSubjects.Clear();
    }
}