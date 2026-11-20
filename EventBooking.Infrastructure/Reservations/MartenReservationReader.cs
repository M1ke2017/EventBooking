using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBooking.Application.Reservations;
using EventBooking.Domain.Reservations;
using Marten;


namespace EventBooking.Infrastructure.Reservations
{
    public class MartenReservationReader(IQuerySession session) : IReservationReader
    {
        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        { return session.Events.AggregateStreamAsync<Reservation>(id, token: cancellationToken); }
    }
}
