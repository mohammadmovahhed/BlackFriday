using BlackFriday.Api.DTOs;
using BlackFriday.Domain.Entities;
using BlackFriday.Domain.Enums;
using BlackFriday.Domain.Tenancy;
using BlackFriday.Infrastructure.Persistence;
using BlackFriday.Infrastructure.Redis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlackFriday.Api.Controllers;

[ApiController]
[Route("v1/admin/sale-events")]
public class SaleEventsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IInventoryService _inventory;
    private readonly ILogger<SaleEventsController> _log;

    public SaleEventsController(AppDbContext db, IInventoryService inventory, ILogger<SaleEventsController> log)
    {
        _db = db;
        _inventory = inventory;
        _log = log;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleEventRequest req, CancellationToken ct)
    {
        var tenantId = Request.Headers["X-Api-Key"].FirstOrDefault() ?? "demo-tenant";
        var tenant = new TenantContext(tenantId, TenantTier.Mid);

        if (req.TotalCapacity <= 0)
            return BadRequest(new { error = "INVALID_CAPACITY" });

        if (req.EndsAt <= req.StartsAt)
            return BadRequest(new { error = "INVALID_TIME_RANGE" });

        var saleEvent = new SaleEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId,
            Sku = req.Sku,
            TotalCapacity = req.TotalCapacity,
            RemainingCapacity = req.TotalCapacity,
            OriginalPrice = req.OriginalPrice,
            DiscountPrice = req.DiscountPrice,
            StartsAt = req.StartsAt,
            EndsAt = req.EndsAt,
            Status = SaleEventStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.SaleEvents.Add(saleEvent);
        await _db.SaveChangesAsync(ct);

        // Initialize Redis counter
        await _inventory.InitializeAsync(tenant, saleEvent.Id, saleEvent.Sku, saleEvent.TotalCapacity, ct);

        _log.LogInformation("SaleEvent {Id} created for tenant {Tenant}",
            saleEvent.Id, tenant.TenantId);

        return Ok(new
        {
            id = saleEvent.Id,
            tenant_id = saleEvent.TenantId,
            sku = saleEvent.Sku,
            total_capacity = saleEvent.TotalCapacity,
            discount_price = saleEvent.DiscountPrice,
            starts_at = saleEvent.StartsAt,
            ends_at = saleEvent.EndsAt,
            status = saleEvent.Status.ToString()
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var tenantId = Request.Headers["X-Api-Key"].FirstOrDefault() ?? "demo-tenant";

        var saleEvent = await _db.SaleEvents
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (saleEvent is null) return NotFound(new { error = "NOT_FOUND" });

        var tenant = new TenantContext(tenantId, TenantTier.Mid);
        var remaining = await _inventory.GetRemainingAsync(tenant, saleEvent.Id, saleEvent.Sku, ct);

        return Ok(new
        {
            id = saleEvent.Id,
            sku = saleEvent.Sku,
            total_capacity = saleEvent.TotalCapacity,
            remaining_stock = remaining,
            status = saleEvent.Status.ToString(),
            starts_at = saleEvent.StartsAt,
            ends_at = saleEvent.EndsAt
        });
    }
}