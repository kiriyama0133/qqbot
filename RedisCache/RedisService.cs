using qqbot.RedisCache;
using StackExchange.Redis;
using System.Text.Json;
using System.Threading.Tasks;

namespace qqbot.RedisCache
{
    /// <summary>
    /// Redis 队列服务（基于 RedisManager 的连接池）
    /// </summary>
    public class RedisService
    {
        private readonly IDatabase _db;

        public RedisService(int db = 0)
        {
            // 直接使用 RedisManager 获取池化的连接
            _db = RedisManager.GetDatabase(db);
        }
        public async Task PushMessageAsync(string queue, object message)
        {
            var json = JsonSerializer.Serialize(message);
            await _db.ListRightPushAsync(queue, json);
        }
        public async Task<string?> PopMessageAsync(string queue)
        {
            var result = await _db.ListLeftPopAsync(queue);
            return result.HasValue ? result.ToString() : null;
        }

        /// <summary>
        /// 阻塞式获取消息
        /// </summary>
        public async Task<string?> BlockPopMessageAsync(string queue, int timeoutSeconds = 0)
        {
            var result = await _db.ListLeftPopAsync(queue);

            // 如果需要真正的阻塞式，可以用 BLPOP（StackExchange.Redis 不直接支持，需要 Execute）
            if (!result.HasValue && timeoutSeconds > 0)
            {
                var redisResult = await _db.ExecuteAsync("BLPOP", queue, timeoutSeconds);
                if (!redisResult.IsNull)
                {
                    // BLPOP 返回
                    return redisResult.ToDictionary()[queue]?.ToString();
                }
            }

            return result.HasValue ? result.ToString() : null;
        }
    }
}
