using qqbot.RedisCache;
using StackExchange.Redis;
using System.Text.Json;
using System.Threading.Tasks;
using qqbot.Models;
using static qqbot.Helper.MessagePrint;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.Tasks;
using static qqbot.Models.Group;
namespace qqbot.RedisCache
{
    public class QueuePrivateMessage
    {
        public string Key { get; set; } = string.Empty;
        public string MessageSender { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime ExpireAt { get; set; }
    }
    public class QueueGroupMessage : QueuePrivateMessage
    {
        public string GroupId { get; set; } = string.Empty;
    }
    /// <summary>
    /// Redis 队列服务（基于 RedisManager 的连接池）
    /// </summary>
    /// 
    public class RedisService
    {
        private readonly IDatabase _db;

        public RedisService(int db = 0)
        {
            var RedisCacheManager = new RedisManager(new Microsoft.Extensions.Options.OptionsWrapper<Models.RedisSetting>(new Models.RedisSetting { Url = "localhost:6379" }));
            _db = RedisCacheManager.GetDatabase(db);
        }
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true, // 格式化缩进（可选）
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        public async Task PushPrivateMessageAsync(string queue, PrivateMessageEvent e)
        {   
            var message = new QueuePrivateMessage
            {
                Key = queue,
                MessageSender = $"{e.UserId}",
                Message = FormatMessageSegments(e.Message),
                ExpireAt = DateTime.UtcNow.AddMinutes(1) // 设置1分钟过期
            };
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            await _db.ListRightPushAsync(queue, json);
        }
         public async Task PushGroupMessageAsync(string queue, GroupMessageEvent e)
        {   
            var message = new QueueGroupMessage
            {
                Key = queue,
                MessageSender = $"{e.UserId}",
                GroupId = $"{e.GroupId}",
                Message = FormatMessageSegments(e.Message),
                ExpireAt = DateTime.UtcNow.AddMinutes(1) // 设置1分钟过期
            };
            var json = JsonSerializer.Serialize(message, _jsonOptions);
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
