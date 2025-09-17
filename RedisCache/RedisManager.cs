using System;
using Microsoft.Extensions.Options;
using qqbot.Models;
using StackExchange.Redis;
namespace qqbot.RedisCache;

public class RedisManager
{
    private Lazy<ConnectionMultiplexer> _lazyConnection;
    private readonly RedisSetting _setting;

    public RedisManager( IOptions<RedisSetting> redisSetting)
    {
        _setting = redisSetting.Value;
        _lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            return ConnectionMultiplexer.Connect(_setting.Url);
        });
    }

    public  ConnectionMultiplexer Connection => _lazyConnection.Value;
    public IDatabase GetDatabase(int db)
    {
        return Connection.GetDatabase(db); 
    } 
}