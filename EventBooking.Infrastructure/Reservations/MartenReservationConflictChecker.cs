using EventBooking.Application.Reservations;
using EventBooking.Domain.Reservations;
using Marten;

namespace EventBooking.Infrastructure.Reservations;

public class MartenReservationConflictChecker(
    IQuerySession session) : IReservationConflictChecker
{
    public Task<bool> HasConflictAsync(
        Guid resourceId,
        DateTimeOffset start,
        DateTimeOffset end,
        Guid? excludedReservationId,
        CancellationToken ct)
    {
        var query = session.Query<Reservation>()
            .Where(r =>
                r.ResourceId == resourceId &&
                !r.IsCancelled &&
                r.Start < end &&
                r.End > start);

        if (excludedReservationId.HasValue)
        {
            var excludedId = excludedReservationId.Value;
            query = query.Where(r => r.Id != excludedId);
        }

        return query.AnyAsync(ct);
    }
}
