using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBooking.Application.Reservations
{
    public interface IReservationHistoryReader
    {
        Task<IReadOnlyList<ReservationHistoryItem>> GetAsync(Guid reservationId,CancellationToken cancellationToken);
    }
}
