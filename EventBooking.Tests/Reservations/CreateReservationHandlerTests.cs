
using EventBooking.Application.Reservations;
using EventBooking.Domain.Reservations.Events;
using Xunit;

namespace EventBooking.Tests.Reservations;

public class CreateReservationHandlerTests
{
    private static readonly Guid ResourceId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly DateTimeOffset Start =
        new(2026, 12, 20, 10, 0, 0, TimeSpan.FromHours(1));

    private static readonly DateTimeOffset End = Start.AddHours(1);

    [Fact]
    public async Task ValidReservation_ShouldSaveCreatedEvent()
    {
        var store = new FakeEventStore();
        var checker = new FakeConflictChecker(false);
        var handler = new CreateReservationHandler(store, checker);

        var id = await handler.HandleAsync(
            new CreateReservation(ResourceId, UserId, Start, End),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        Assert.NotNull(store.SavedEvent);
        Assert.Equal(id, store.SavedEvent.ReservationId);
        Assert.Equal(ResourceId, store.SavedEvent.ResourceId);
        Assert.Equal(UserId, store.SavedEvent.UserId);
    }

    [Fact]
    public async Task InvalidDateRange_ShouldBeRejected()
    {
        var store = new FakeEventStore();
        var handler = new CreateReservationHandler(
            store, new FakeConflictChecker(false));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(
                new CreateReservation(ResourceId, UserId, End, Start),
                CancellationToken.None));

        Assert.Null(store.SavedEvent);
    }

    [Fact]
    public async Task EmptyResourceId_ShouldBeRejected()
    {
        var store = new FakeEventStore();
        var handler = new CreateReservationHandler(
            store, new FakeConflictChecker(false));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(
                new CreateReservation(Guid.Empty, UserId, Start, End),
                CancellationToken.None));

        Assert.Null(store.SavedEvent);
    }

    [Fact]
    public async Task ConflictingReservation_ShouldBeRejected()
    {
        var store = new FakeEventStore();
        var handler = new CreateReservationHandler(
            store, new FakeConflictChecker(true));

        await Assert.ThrowsAsync<ReservationConflictException>(() =>
            handler.HandleAsync(
                new CreateReservation(ResourceId, UserId, Start, End),
                CancellationToken.None));

        Assert.Null(store.SavedEvent);
    }

    private class FakeConflictChecker(bool hasConflict)
        : IReservationConflictChecker
    {
        public Task<bool> HasConflictAsync(
            Guid resourceId,
            DateTimeOffset start,
            DateTimeOffset end,
            Guid? excludedReservationId,
            CancellationToken ct)
        {
            return Task.FromResult(hasConflict);
        }
    }

    private class FakeEventStore : IReservationEventStore
    {
        public ReservationCreated? SavedEvent { get; private set; }

        public Task SaveAsync(
            ReservationCreated reservationEvent,
            CancellationToken ct)
        {
            SavedEvent = reservationEvent;
            return Task.CompletedTask;
        }

        public Task CancelAsync(Guid id, CancellationToken ct)
            => Task.CompletedTask;

        public Task RescheduleAsync(
            Guid id,
            DateTimeOffset start,
            DateTimeOffset end,
            CancellationToken ct)
            => Task.CompletedTask;

        public Task ChangeResourceAsync(
            Guid id,
            Guid resourceId,
            CancellationToken ct)
            => Task.CompletedTask;
    }
}
