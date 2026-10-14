using BlackFriday.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlackFriday.Infrastructure.Persistence.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            builder.Property(x => x.CustomerId).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.Status).HasConversion<int>();

            builder.HasIndex(x => x.ReservationId).IsUnique();
            builder.HasIndex(x => new { x.TenantId, x.Status });
        }
    }
}