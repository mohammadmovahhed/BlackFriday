using BlackFriday.Domain.Enums;

namespace BlackFriday.Domain.Entities
{
    public class Reservation
    {
        public Guid Id { get; set; }
        public string TenantId { get; set; } = default!;
        public string Token { get; set; } = default!;
        public Guid SaleEventId { get; set; }
        public string Sku { get; set; } = default!;
        public int Quantity { get; set; }
        public string CustomerId { get; set; } = default!;
        public ReservationStatus Status { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}