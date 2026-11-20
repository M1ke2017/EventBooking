using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBooking.Domain.Reservations.Events
{
    public record ReservationCreated(Guid ReservationId, Guid ResourceId, Guid UserId, DateTimeOffset Start, DateTimeOffset End);
    
}
