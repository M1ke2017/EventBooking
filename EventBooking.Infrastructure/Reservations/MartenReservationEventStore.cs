using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBooking.Application.Reservations;
using EventBooking.Domain.Reservations;
using EventBooking.Domain.Reservations.Events;
using Marten;

namespace EventBooking.Infrastructure.Reservations
{
    public class MartenReservationEventStore(IDocumentSession session) : IReservationEventStore
    {
        public async Task SaveAsync(ReservationCreated reservationEvent, CancellationToken cancellationToken)
        { session.Events.StartStream<Reservation>(reservationEvent.ReservationId, reservationEvent); await session.SaveChangesAsync(cancellationToken); }

        public async Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
        { session.Events.Append(reservationId, new ReservationCancelled(reservationId)); await session.SaveChangesAsync(cancellationToken); }
        public async Task RescheduleAsync(Guid reservationId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
        { session.Events.Append(reservationId, new ReservationRescheduled(reservationId, start, end)); await session.SaveChangesAsync(cancellationToken); }
        public async Task ChangeResourceAsync(Guid reservationId, Guid resourceId ,CancellationToken cancellationToken)
        { session.Events.Append(reservationId, new ReservationResourceChanged(reservationId, resourceId)); await session.SaveChangesAsync(cancellationToken); }


    }
}
