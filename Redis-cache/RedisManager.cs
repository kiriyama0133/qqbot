using System;
using StackExchange.Redis;
namespace qqbot.RedisCache
{
    public class RedisManager
    {
        private static Lazy<ConnectionMultiplexer> lazyConnection;

        static RedisManager()
        {
            lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                return ConnectionMultiplexer.Connect("localhost:6379");
            });
        }

        public static ConnectionMultiplexer Connection => lazyConnection.Value;
        public static IDatabase GetDatabase(int db)
        {
            return Connection.GetDatabase(db); 
        } 
    }
}