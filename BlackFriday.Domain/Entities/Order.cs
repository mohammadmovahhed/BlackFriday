using BlackFriday.Domain.Enums;

namespace BlackFriday.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public Guid ReservationId { get; set; }
    public string CustomerId { get; set; } = default!;
    public decimal Amount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}