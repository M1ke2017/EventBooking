using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBooking.Domain.Reservations.Events;

namespace EventBooking.Application.Reservations
{
    public interface IReservationEventStore
    { 
        Task SaveAsync(ReservationCreated reservationEvent, CancellationToken cancellationToken); 
        Task CancelAsync(Guid reservationId, CancellationToken cancellationToken);
        Task RescheduleAsync(Guid reservationId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken);
        Task ChangeResourceAsync(Guid reservationId, Guid resourceId, CancellationToken cancellationToken);
    }
}
