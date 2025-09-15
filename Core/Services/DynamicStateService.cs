using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace qqbot.Core.Services
{
    public class DynamicStateService : IDynamicStateService, IDisposable
    {
        // 并发字典来线程安全地存储每个状态键的“主题”
        private readonly ConcurrentDictionary<string, ISubject<object>> _stateSubjects
            = new ConcurrentDictionary<string, ISubject<object>>();

        public void SetState(string key, object value)
        {
            // GetOrAdd 确保了只有一个线程能成功创建 Subject
            var subject = _stateSubjects.GetOrAdd(key, _ => new BehaviorSubject<object>(value));
            subject.OnNext(value); // 推送新的值
        }

        public T? GetState<T>(string key, T? defaultValue = default)
        {
            if (_stateSubjects.TryGetValue(key, out var subject) && subject is BehaviorSubject<object> behaviorSubject)
            {
                // 尝试获取 BehaviorSubject 的最新值
                if (behaviorSubject.TryGetValue(out var value))
                {
                    return value is T typedValue ? typedValue : defaultValue;
                }
            }
            return defaultValue;
        }

        public IObservable<T> GetStateObservable<T>(string key)
        {
            // 获取或创建对应键的 Subject，并返回它的可观察序列
            var subject = _stateSubjects.GetOrAdd(key, _ => new BehaviorSubject<object>(default(T)));

            // 使用 OfType<T> 来安全地进行类型过滤和转换
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
}
