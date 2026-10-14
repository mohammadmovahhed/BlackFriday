using BlackFriday.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlackFriday.Infrastructure.Persistence.Repositories
{
    public interface IReservationRepository
    {
        Task AddAsync(Reservation reservation, CancellationToken ct = default);
        Task<Reservation?> GetByTokenAsync(string tenantId, string token, CancellationToken ct = default);
    }

    public class ReservationRepository : IReservationRepository
    {
        private readonly AppDbContext _db;

        public ReservationRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Reservation reservation, CancellationToken ct = default)
        {
            _db.Reservations.Add(reservation);
            await _db.SaveChangesAsync(ct);
        }

        public Task<Reservation?> GetByTokenAsync(string tenantId, string token, CancellationToken ct = default)
            => _db.Reservations.FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.Token == token, ct);
    }
}