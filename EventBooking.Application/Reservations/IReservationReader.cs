using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBooking.Domain.Reservations;

namespace EventBooking.Application.Reservations
{
    public interface IReservationReader
    { Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken); }
}
