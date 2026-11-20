using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBooking.Application.Reservations
{
    public record ReservationHistoryItem(long Version, string EventType, DateTimeOffset Timestamp, object Data);
}
