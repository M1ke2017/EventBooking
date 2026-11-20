using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBooking.Application.Reservations
{
    public interface IReservationConflictChecker
    { Task<bool> HasConflictAsync(Guid resourceId, DateTimeOffset start, DateTimeOffset end, Guid? excludedReservationId, CancellationToken ct); }
    public class ReservationConflictException: Exception
    {
        public ReservationConflictException(): base("Zasób jest już zarezerwowany w wybranym terminie.")
        {}
    }
}
