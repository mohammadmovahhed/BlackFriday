using BlackFriday.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlackFriday.Infrastructure.Persistence.Configurations
{
    public class SaleEventConfiguration : IEntityTypeConfiguration<SaleEvent>
    {
        public void Configure(EntityTypeBuilder<SaleEvent> builder)
        {
            builder.ToTable("SaleEvents");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Sku).HasMaxLength(128).IsRequired();
            builder.Property(x => x.OriginalPrice).HasPrecision(18, 2);
            builder.Property(x => x.DiscountPrice).HasPrecision(18, 2);
            builder.Property(x => x.Status).HasConversion<int>();

            builder.HasIndex(x => new { x.TenantId, x.Sku });
            builder.HasIndex(x => new { x.TenantId, x.Status, x.StartsAt });
        }
    }
}