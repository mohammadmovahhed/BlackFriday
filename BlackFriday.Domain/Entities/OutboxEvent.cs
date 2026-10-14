using BlackFriday.Domain.Enums;

namespace BlackFriday.Domain.Entities
{
    public class OutboxEvent
    {
        public Guid Id { get; set; }
        public string TenantId { get; set; } = default!;
        public string EventType { get; set; } = default!;
        public string Payload { get; set; } = default!;
        public OutboxStatus Status { get; set; }
        public int RetryCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
    }
}