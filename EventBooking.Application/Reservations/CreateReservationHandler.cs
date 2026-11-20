using EventBooking.Domain.Reservations;
using EventBooking.Domain.Reservations.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBooking.Application.Reservations
{
    public class CreateReservationHandler(IReservationEventStore eventStore, IReservationConflictChecker conflictChecker)
    {
        public async Task<Guid> HandleAsync(CreateReservation command, CancellationToken cancellationToken)
        {
            if (command.ResourceId == Guid.Empty || command.UserId == Guid.Empty)
            { throw new ArgumentException("ResourceId and UserId are required."); }
            if (command.Start >= command.End)
            { throw new ArgumentException("End must be later than Start."); }
            if (await conflictChecker.HasConflictAsync(command.ResourceId, command.Start, command.End, null, cancellationToken))
            { throw new ReservationConflictException(); }
            var reservationId = Guid.NewGuid();
            var reservationEvent = new ReservationCreated(reservationId, command.ResourceId, command.UserId, command.Start, command.End);
            await eventStore.SaveAsync(reservationEvent, cancellationToken);
            return reservationId;
        }
    }
}
