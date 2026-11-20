namespace EventBooking.Application.Reservations;

public record ReservedPeriod(
    Guid ReservationId,
    Guid ResourceId,
    Guid UserId,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsCancelled);

public interface IReservationCalendarReader
{
    Task<IReadOnlyList<ReservedPeriod>> GetAsync(
        Guid resourceId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);
}
