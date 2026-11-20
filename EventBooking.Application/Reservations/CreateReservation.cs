using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBooking.Application.Reservations
{
    public record CreateReservation(Guid ResourceId, Guid UserId, DateTimeOffset Start, DateTimeOffset End);
}
