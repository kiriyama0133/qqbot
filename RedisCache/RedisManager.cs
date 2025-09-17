using System;
using Microsoft.Extensions.Options;
using qqbot.Models;
using StackExchange.Redis;
namespace qqbot.RedisCache;

public class RedisManager
{
    private static Lazy<ConnectionMultiplexer> lazyConnection;
    private readonly RedisSetting _setting;

    public RedisManager( IOptions<RedisSetting> redisSetting)
    {
        _setting = redisSetting.Value;
        lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            return ConnectionMultiplexer.Connect(_setting.Url);
        });
    }

    public static ConnectionMultiplexer Connection => lazyConnection.Value;
    public static IDatabase GetDatabase(int db)
    {
        return Connection.GetDatabase(db); 
    } 
}