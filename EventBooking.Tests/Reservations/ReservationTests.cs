
using EventBooking.Domain.Reservations;
using EventBooking.Domain.Reservations.Events;
using Xunit;

namespace EventBooking.Tests.Reservations;

public class ReservationTests
{
    private static readonly Guid ReservationId = Guid.NewGuid();
    private static readonly Guid ResourceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly DateTimeOffset Start =
        new(2026, 12, 15, 10, 0, 0, TimeSpan.FromHours(1));

    private static readonly DateTimeOffset End =
        new(2026, 12, 15, 11, 0, 0, TimeSpan.FromHours(1));

    [Fact]
    public void CreatedEvent_ShouldInitializeReservation()
    {
        var reservation = new Reservation();

        reservation.Apply(new ReservationCreated(
            ReservationId, ResourceId, UserId, Start, End));

        Assert.Equal(ReservationId, reservation.Id);
        Assert.Equal(ResourceId, reservation.ResourceId);
        Assert.Equal(UserId, reservation.UserId);
        Assert.Equal(Start, reservation.Start);
        Assert.Equal(End, reservation.End);
        Assert.False(reservation.IsCancelled);
    }

    [Fact]
    public void CancelledEvent_ShouldCancelReservation()
    {
        var reservation = CreateReservation();

        reservation.Apply(new ReservationCancelled(
            ReservationId));

        Assert.True(reservation.IsCancelled);
    }

    [Fact]
    public void RescheduledEvent_ShouldUpdateDates()
    {
        var reservation = CreateReservation();

        var newStart = Start.AddHours(2);
        var newEnd = End.AddHours(2);

        reservation.Apply(new ReservationRescheduled(
            ReservationId, newStart, newEnd));

        Assert.Equal(newStart, reservation.Start);
        Assert.Equal(newEnd, reservation.End);
    }

    [Fact]
    public void ResourceChangedEvent_ShouldUpdateResource()
    {
        var reservation = CreateReservation();
        var newResourceId = Guid.NewGuid();

        reservation.Apply(new ReservationResourceChanged(
            ReservationId, newResourceId));

        Assert.Equal(newResourceId, reservation.ResourceId);
    }

    [Fact]
    public void EventReplay_ShouldRestoreFinalState()
    {
        var newResourceId = Guid.NewGuid();

        var reservation = new Reservation();

        reservation.Apply(new ReservationCreated(
            ReservationId, ResourceId, UserId, Start, End));

        reservation.Apply(new ReservationRescheduled(
            ReservationId,
            Start.AddHours(3),
            End.AddHours(3)));

        reservation.Apply(new ReservationResourceChanged(
            ReservationId, newResourceId));

        reservation.Apply(new ReservationCancelled(
            ReservationId));

        Assert.Equal(ReservationId, reservation.Id);
        Assert.Equal(newResourceId, reservation.ResourceId);
        Assert.Equal(Start.AddHours(3), reservation.Start);
        Assert.Equal(End.AddHours(3), reservation.End);
        Assert.True(reservation.IsCancelled);
    }

    private static Reservation CreateReservation()
    {
        var reservation = new Reservation();

        reservation.Apply(new ReservationCreated(
            ReservationId, ResourceId, UserId, Start, End));

        return reservation;
    }
}
