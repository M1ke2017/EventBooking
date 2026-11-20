
using System.Text.Json.Serialization;
using EventBooking.Domain.Reservations.Events;

namespace EventBooking.Domain.Reservations;

public class Reservation
{
    [JsonInclude]
    public Guid Id { get; private set; }

    [JsonInclude]
    public Guid ResourceId { get; private set; }

    [JsonInclude]
    public Guid UserId { get; private set; }

    [JsonInclude]
    public DateTimeOffset Start { get; private set; }

    [JsonInclude]
    public DateTimeOffset End { get; private set; }

    [JsonInclude]
    public bool IsCancelled { get; private set; }

    public void Apply(ReservationCreated e)
    {
        Id = e.ReservationId;
        ResourceId = e.ResourceId;
        UserId = e.UserId;
        Start = e.Start;
        End = e.End;
        IsCancelled = false;
    }

    public void Apply(ReservationCancelled e)
    { IsCancelled = true; }

    public void Apply(ReservationRescheduled e)
    { Start = e.Start; End = e.End; }

    public void Apply(ReservationResourceChanged e)
    { ResourceId = e.ResourceId; }
}
