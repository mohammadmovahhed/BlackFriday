using BlackFriday.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlackFriday.Infrastructure.Persistence.Configurations
{
    public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
    {
        public void Configure(EntityTypeBuilder<Reservation> builder)
        {
            builder.ToTable("Reservations");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Token).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Sku).HasMaxLength(128).IsRequired();
            builder.Property(x => x.CustomerId).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Status).HasConversion<int>();

            builder.HasIndex(x => x.Token).IsUnique();
            builder.HasIndex(x => new { x.TenantId, x.Status, x.ExpiresAt });

            builder.HasOne<SaleEvent>()
                .WithMany()
                .HasForeignKey(x => x.SaleEventId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}