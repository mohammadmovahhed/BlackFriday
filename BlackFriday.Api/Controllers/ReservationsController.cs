using BlackFriday.Api.DTOs;
using BlackFriday.Domain.Entities;
using BlackFriday.Domain.Enums;
using BlackFriday.Domain.Tenancy;
using BlackFriday.Infrastructure.Persistence;
using BlackFriday.Infrastructure.Persistence.Repositories;
using BlackFriday.Infrastructure.Redis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlackFriday.Api.Controllers;

[ApiController]
[Route("v1/reservations")]
public class ReservationsController : ControllerBase
{
    private static readonly TimeSpan ReservationTtl = TimeSpan.FromMinutes(15);

    private readonly AppDbContext _db;
    private readonly IInventoryService _inventory;
    private readonly IReservationRepository _reservations;
    private readonly ILogger<ReservationsController> _log;

    public ReservationsController(
        AppDbContext db,
        IInventoryService inventory,
        IReservationRepository reservations,
        ILogger<ReservationsController> log)
    {
        _db = db;
        _inventory = inventory;
        _reservations = reservations;
        _log = log;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest req, CancellationToken ct)
    {
        var tenantId = Request.Headers["X-Api-Key"].FirstOrDefault() ?? "demo-tenant";
        var tenant = new TenantContext(tenantId, TenantTier.Mid);

        if (req.Quantity <= 0)
            return BadRequest(new { error = "INVALID_QUANTITY" });

        // 1. Load sale event (must belong to tenant)
        var saleEvent = await _db.SaleEvents
            .FirstOrDefaultAsync(x => x.Id == req.SaleEventId && x.TenantId == tenantId, ct);

        if (saleEvent is null)
            return NotFound(new { error = "PROMO_NOT_FOUND" });

        if (saleEvent.Status != SaleEventStatus.Active)
            return Conflict(new { error = "PROMO_NOT_ACTIVE" });

        var now = DateTimeOffset.UtcNow;
        if (now < saleEvent.StartsAt)
            return Conflict(new { error = "PROMO_NOT_STARTED" });
        if (now > saleEvent.EndsAt)
            return Conflict(new { error = "PROMO_ENDED" });

        // 2. Atomic reserve in Redis
        var token = Guid.NewGuid().ToString("N");
        var result = await _inventory.ReserveAsync(
            tenant, saleEvent.Id, saleEvent.Sku, token, req.Quantity, ReservationTtl, ct);

        if (!result.Success)
        {
            _log.LogWarning("Sold out for tenant {Tenant} promo {Promo}",
                tenant.TenantId, saleEvent.Id);

            return Conflict(new
            {
                error = "SOLD_OUT",
                remaining_stock = Math.Max(0, result.Remaining)
            });
        }

        // 3. Persist reservation
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.TenantId,
            Token = token,
            SaleEventId = saleEvent.Id,
            Sku = saleEvent.Sku,
            Quantity = req.Quantity,
            CustomerId = req.CustomerId,
            Status = ReservationStatus.Pending,
            ExpiresAt = now.Add(ReservationTtl),
            CreatedAt = now
        };

        await _reservations.AddAsync(reservation, ct);

        _log.LogInformation(
            "Reservation {Token} created for tenant {Tenant} qty {Qty} remaining {Remaining}",
            token, tenant.TenantId, req.Quantity, result.Remaining);

        return Ok(new
        {
            reservation_token = token,
            status = "PENDING",
            expires_at = reservation.ExpiresAt,
            remaining_stock = result.Remaining
        });
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> Get(string token, CancellationToken ct)
    {
        var tenantId = Request.Headers["X-Api-Key"].FirstOrDefault() ?? "demo-tenant";

        var reservation = await _reservations.GetByTokenAsync(tenantId, token, ct);
        if (reservation is null)
            return NotFound(new { error = "NOT_FOUND" });

        return Ok(new
        {
            reservation_token = reservation.Token,
            status = reservation.Status.ToString(),
            sku = reservation.Sku,
            quantity = reservation.Quantity,
            expires_at = reservation.ExpiresAt,
            created_at = reservation.CreatedAt
        });
    }
}