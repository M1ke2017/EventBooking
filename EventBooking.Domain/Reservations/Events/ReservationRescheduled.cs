using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBooking.Domain.Reservations.Events
{
    public record ReservationRescheduled(Guid ReservationId, DateTimeOffset Start, DateTimeOffset End);
}
