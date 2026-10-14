using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace BlackFriday.Infrastructure.Redis;

public interface IRedisConnection
{
    IConnectionMultiplexer Multiplexer { get; }
}

public class RedisConnection : IRedisConnection, IDisposable
{
    private readonly Lazy<ConnectionMultiplexer> _lazy;

    public RedisConnection(IConfiguration config)
    {
        var connStr = config.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string missing");

        _lazy = new Lazy<ConnectionMultiplexer>(() =>
        {
            var options = ConfigurationOptions.Parse(connStr);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });
    }

    public IConnectionMultiplexer Multiplexer => _lazy.Value;

    public void Dispose()
    {
        if (_lazy.IsValueCreated)
            _lazy.Value.Dispose();
    }
}