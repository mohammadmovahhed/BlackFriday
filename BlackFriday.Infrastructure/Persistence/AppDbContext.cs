using BlackFriday.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlackFriday.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<SaleEvent> SaleEvents => Set<SaleEvent>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}