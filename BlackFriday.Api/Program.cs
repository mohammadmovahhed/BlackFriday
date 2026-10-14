using BlackFriday.Infrastructure.Persistence;
using BlackFriday.Infrastructure.Persistence.Repositories;
using BlackFriday.Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using BlackFriday.Domain.Entities;
using BlackFriday.Domain.Enums;
using BlackFriday.Domain.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Sql")));

builder.Services.AddSingleton<IRedisConnection, RedisConnection>();
builder.Services.AddSingleton<IInventoryService, InventoryService>();

builder.Services.AddScoped<IReservationRepository, ReservationRepository>();

var app = builder.Build();

// Auto-migrate + Seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var inventory = scope.ServiceProvider.GetRequiredService<IInventoryService>();
    db.Database.Migrate();
    await DataSeeder.SeedAsync(db, inventory);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();




// ============ SEEDER ============
public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db, IInventoryService inventory)
    {
        if (await db.SaleEvents.AnyAsync()) return; // idempotent

        var now = DateTimeOffset.UtcNow;
        var tenants = new[] { "tenant-electronics", "tenant-fashion" };
        var skus = new (string sku, decimal orig, decimal disc)[]
        {
            ("TV-65-4K", 1999m, 1299m),
            ("TV-55-4K", 1499m, 999m),
            ("LAPTOP-PRO", 2499m, 1799m),
            ("PHONE-X", 999m, 699m),
            ("HEADPHONES", 299m, 149m),
            ("JACKET-WINTER", 199m, 99m),
            ("SHOES-RUN", 149m, 79m),
            ("JEANS-SLIM", 89m, 49m),
            ("TSHIRT-BASIC", 39m, 19m),
            ("BAG-LEATHER", 249m, 129m)
        };

        var events = new List<SaleEvent>();
        for (int i = 0; i < skus.Length; i++)
        {
            var tenant = tenants[i % 2];
            var status = i % 3;
            var startsAt = status switch
            {
                0 => now.AddHours(-1),   // active
                1 => now.AddDays(1),     // future
                _ => now.AddDays(-2)     // past
            };
            var endsAt = status switch
            {
                0 => now.AddDays(7),
                1 => now.AddDays(8),
                _ => now.AddDays(-1)
            };
            events.Add(new SaleEvent
            {
                Id = Guid.NewGuid(),
                TenantId = tenant,
                Sku = skus[i].sku,
                TotalCapacity = 100,
                RemainingCapacity = 100,
                OriginalPrice = skus[i].orig,
                DiscountPrice = skus[i].disc,
                StartsAt = startsAt,
                EndsAt = endsAt,
                Status = status == 0 ? SaleEventStatus.Active : SaleEventStatus.Draft,
                CreatedAt = now
            });
        }

        db.SaleEvents.AddRange(events);
        await db.SaveChangesAsync();

        foreach (var ev in events.Where(e => e.Status == SaleEventStatus.Active))
        {
            var tenant = new TenantContext(ev.TenantId, TenantTier.Mid);
            await inventory.InitializeAsync(tenant, ev.Id, ev.Sku, ev.TotalCapacity);
        }
    }
}