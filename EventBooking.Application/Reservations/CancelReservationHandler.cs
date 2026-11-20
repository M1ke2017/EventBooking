
namespace EventBooking.Application.Reservations;

public class CancelReservationHandler(
    IReservationReader reader,
    IReservationEventStore eventStore)
{
    public async Task HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var reservation = await reader.GetByIdAsync(
            id, cancellationToken);

        if (reservation is null)
            throw new KeyNotFoundException(
                "Reservation not found.");

        if (reservation.IsCancelled)
            throw new InvalidOperationException(
                "Reservation is already cancelled.");

        if (reservation.Start <
            DateTimeOffset.UtcNow.AddMinutes(5))
        {
            throw new InvalidOperationException(
                "Rezerwację można anulować najpóźniej 5 minut przed rozpoczęciem.");
        }

        await eventStore.CancelAsync(
            id, cancellationToken);
    }
}
