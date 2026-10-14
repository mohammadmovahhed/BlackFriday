using BlackFriday.Domain.Tenancy;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BlackFriday.Infrastructure.Redis;

public readonly record struct ReserveResult(bool Success, long Remaining)
{
    public static ReserveResult Ok(long remaining) => new(true, remaining);
    public static ReserveResult SoldOut(long remaining) => new(false, remaining);
    public static ReserveResult NotInitialized() => new(false, -1);
}

public interface IInventoryService
{
    Task InitializeAsync(TenantContext tenant, Guid promoId, string sku, int capacity, CancellationToken ct = default);
    Task<ReserveResult> ReserveAsync(TenantContext tenant, Guid promoId, string sku, string token, int qty, TimeSpan ttl, CancellationToken ct = default);
    Task<long> GetRemainingAsync(TenantContext tenant, Guid promoId, string sku, CancellationToken ct = default);
}

public class InventoryService : IInventoryService
{
    private readonly IRedisConnection _redis;
    private readonly ILogger<InventoryService> _log;
    private static readonly string ReserveScript = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Redis", "Scripts", "reserve.lua"));

    public InventoryService(IRedisConnection redis, ILogger<InventoryService> log)
    {
        _redis = redis;
        _log = log;
    }

    public async Task InitializeAsync(TenantContext tenant, Guid promoId, string sku, int capacity, CancellationToken ct = default)
    {
        var db = _redis.Multiplexer.GetDatabase();
        var key = tenant.RedisKey("promo", promoId.ToString(), "sku", sku, "remaining");
        await db.StringSetAsync(key, capacity);
    }

    public async Task<ReserveResult> ReserveAsync(TenantContext tenant, Guid promoId, string sku, string token, int qty, TimeSpan ttl, CancellationToken ct = default)
    {
        var db = _redis.Multiplexer.GetDatabase();
        var keys = new RedisKey[]
        {
            tenant.RedisKey("promo", promoId.ToString(), "sku", sku, "remaining"),
            tenant.RedisKey("reservation", token),
            tenant.RedisKey("promo", promoId.ToString(), "sku", sku, "active"),
        };
        var args = new RedisValue[] { qty, (int)ttl.TotalSeconds, token };

        var result = (RedisValue[]?)await db.ScriptEvaluateAsync(ReserveScript, keys, args);

        if (result is null || result.Length == 0)
        {
            _log.LogError("Empty Lua result for tenant {Tenant}", tenant.TenantId);
            return ReserveResult.NotInitialized();
        }

        var status = (long)result[0];
        var remaining = (long)result[1];

        return status switch
        {
            1 => ReserveResult.Ok(remaining),
            0 => ReserveResult.SoldOut(remaining),
            _ => ReserveResult.NotInitialized()
        };
    }

    public async Task<long> GetRemainingAsync(TenantContext tenant, Guid promoId, string sku, CancellationToken ct = default)
    {
        var db = _redis.Multiplexer.GetDatabase();
        var key = tenant.RedisKey("promo", promoId.ToString(), "sku", sku, "remaining");
        var val = await db.StringGetAsync(key);
        return val.HasValue ? (long)val : -1;
    }
}