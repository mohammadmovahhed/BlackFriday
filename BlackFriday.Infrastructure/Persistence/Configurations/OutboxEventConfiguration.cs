using BlackFriday.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlackFriday.Infrastructure.Persistence.Configurations
{
    public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
    {
        public void Configure(EntityTypeBuilder<OutboxEvent> builder)
        {
            builder.ToTable("OutboxEvents");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            builder.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Payload).IsRequired();
            builder.Property(x => x.Status).HasConversion<int>();

            builder.HasIndex(x => new { x.Status, x.CreatedAt });
            builder.HasIndex(x => x.TenantId);
        }
    }
}