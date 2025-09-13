namespace qqbot.Core.Services
{
    public interface IDynamicStateService
    {
        /// <summary>
        /// 设置一个状态的值。如果键不存在，则创建它。
        /// </summary>
        void SetState(string key, object value);

        /// <summary>
        /// 获取一个状态的当前值。如果不存在，则返回默认值。
        /// </summary>
        T? GetState<T>(string key, T? defaultValue = default);

        /// <summary>
        /// 获取一个特定状态键的“可观察”对象。
        /// 您可以订阅它，以便在该状态发生变化时立即收到通知。
        /// </summary>
        IObservable<T> GetStateObservable<T>(string key);
    }
}
