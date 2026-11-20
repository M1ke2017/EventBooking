
using EventBooking.Application.Reservations;
using EventBooking.Domain.Reservations;
using Marten;

namespace EventBooking.Infrastructure.Reservations;

public class MartenReservationCalendarReader(
    IQuerySession session) : IReservationCalendarReader
{
    public async Task<IReadOnlyList<ReservedPeriod>> GetAsync(
        Guid resourceId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        var reservations = await session.Query<Reservation>()
            .Where(r =>
                r.ResourceId == resourceId &&
                r.Start < to &&
                r.End > from)
            .OrderBy(r => r.Start)
            .ToListAsync(ct);

        return reservations.Select(r => new ReservedPeriod(
            r.Id,
            r.ResourceId,
            r.UserId,
            r.Start,
            r.End,
            r.IsCancelled)).ToList();
    }
}
