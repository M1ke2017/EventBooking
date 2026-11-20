using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBooking.Application.Reservations;
using Marten;

namespace EventBooking.Infrastructure.Reservations
{
    public class MartenReservationHistoryReader(IQuerySession session) : IReservationHistoryReader
    {
        public async Task<IReadOnlyList<ReservationHistoryItem>> GetAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            var events = await session.Events.FetchStreamAsync(reservationId, token: cancellationToken);
            return events.OrderBy(e => e.Version).Select(e => new ReservationHistoryItem(e.Version, e.Data.GetType().Name, e.Timestamp, e.Data)).ToList();
        }
    }
}
