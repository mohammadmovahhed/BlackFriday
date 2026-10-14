using BlackFriday.Domain.Enums;

namespace BlackFriday.Domain.Entities
{
    public class SaleEvent
    {
        public Guid Id { get; set; }
        public string TenantId { get; set; } = default!;
        public string Sku { get; set; } = default!;
        public int TotalCapacity { get; set; }
        public int RemainingCapacity { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public DateTimeOffset StartsAt { get; set; }
        public DateTimeOffset EndsAt { get; set; }
        public SaleEventStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}