namespace BlackFriday.Domain.Tenancy
{
    public enum TenantTier
    {
        Enterprise = 0,
        Mid = 1,
        Small = 2
    }

    public sealed record TenantContext(string TenantId, TenantTier Tier)
    {
        public string RedisKey(params string[] parts)
            => $"t:{TenantId}:{string.Join(':', parts)}";

        public string StreamKey(string kind)
            => $"t:{TenantId}:stream:{kind}";
    }
}