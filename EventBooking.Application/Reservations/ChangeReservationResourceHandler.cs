using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBooking.Application.Reservations
{
    public class ChangeReservationResourceHandler(IReservationReader reader, IReservationEventStore eventStore, IReservationConflictChecker conflictChecker)
    {
        public async Task HandleAsync(Guid id, Guid resourceId, CancellationToken cancellationToken)
        {
            var reservation = await reader.GetByIdAsync(id, cancellationToken);
            if (reservation is null) throw new KeyNotFoundException("Reservation not found.");
            if (reservation.IsCancelled) throw new InvalidOperationException("Cancelled reservation cannot be modified.");
            if (resourceId == Guid.Empty) throw new ArgumentException("ResourceId is required.");
            if (reservation.ResourceId == resourceId) throw new InvalidOperationException("Reservation already uses this resource.");
            if (await conflictChecker.HasConflictAsync(resourceId, reservation.Start, reservation.End, id, cancellationToken))
            { throw new ReservationConflictException(); }
            await eventStore.ChangeResourceAsync(id, resourceId, cancellationToken);
        }
    }
}
