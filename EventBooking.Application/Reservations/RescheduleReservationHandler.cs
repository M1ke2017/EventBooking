using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBooking.Domain.Reservations.Events;

namespace EventBooking.Application.Reservations
{
    public class RescheduleReservationHandler(IReservationReader reader, IReservationEventStore eventStore, IReservationConflictChecker conflictChecker)
    {
        public async Task HandleAsync(Guid id, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
        {
            var reservation = await reader.GetByIdAsync(id, cancellationToken);
            if (reservation is null) throw new KeyNotFoundException("Reservation not found.");
            if (reservation.IsCancelled) throw new InvalidOperationException("Cancelled reservation cannot be rescheduled.");
            if (start >= end) throw new ArgumentException("End must be later then Start.");
            if (await conflictChecker.HasConflictAsync(reservation.ResourceId, start, end, id, cancellationToken))
            { throw new ReservationConflictException(); }
            await eventStore.RescheduleAsync(id, start, end, cancellationToken);
        }
    }
}
