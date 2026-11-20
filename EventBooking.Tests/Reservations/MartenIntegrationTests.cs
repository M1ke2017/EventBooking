using EventBooking.Domain.Reservations;
using EventBooking.Domain.Reservations.Events;
using JasperFx.Events.Projections;
using Marten;
using Marten.Events.Projections;
using Testcontainers.PostgreSql;
using Xunit;

namespace EventBooking.Tests.Reservations;

public class MartenIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres =
        new PostgreSqlBuilder("postgres:17")
            .Build();

    private IDocumentStore store = null!;

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();

        store = DocumentStore.For(options =>
        {
            options.Connection(postgres.GetConnectionString());

            options.Projections.Snapshot<Reservation>(
                SnapshotLifecycle.Inline);
        });
    }

    [Fact]
    public async Task CreatedEvent_ShouldPersistAndRestoreReservation()
    {
        var id = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var start = new DateTimeOffset(
            2026, 12, 20, 10, 0, 0, TimeSpan.FromHours(1));

        var end = start.AddHours(1);

        await using (var session = store.LightweightSession())
        {
            session.Events.StartStream<Reservation>(
                id,
                new ReservationCreated(
                    id, resourceId, userId, start, end));

            await session.SaveChangesAsync();
        }

        await using var readSession = store.QuerySession();

        var reservation = await readSession.Events
            .AggregateStreamAsync<Reservation>(id);

        Assert.NotNull(reservation);
        Assert.Equal(id, reservation.Id);
        Assert.Equal(resourceId, reservation.ResourceId);
        Assert.Equal(userId, reservation.UserId);
        Assert.Equal(start, reservation.Start);
        Assert.Equal(end, reservation.End);
        Assert.False(reservation.IsCancelled);
    }

    [Fact]
    public async Task CancelledEvent_ShouldUpdateProjection()
    {
        var id = Guid.NewGuid();

        var start = new DateTimeOffset(
            2026, 12, 21, 10, 0, 0, TimeSpan.FromHours(1));

        await using (var session = store.LightweightSession())
        {
            session.Events.StartStream<Reservation>(
                id,
                new ReservationCreated(
                    id,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    start,
                    start.AddHours(1)));

            await session.SaveChangesAsync();
        }

        await using (var session = store.LightweightSession())
        {
            session.Events.Append(
                id,
                new ReservationCancelled(id));

            await session.SaveChangesAsync();
        }

        await using var readSession = store.QuerySession();

        var reservation = await readSession
            .LoadAsync<Reservation>(id);

        var events = await readSession.Events
            .FetchStreamAsync(id);

        Assert.NotNull(reservation);
        Assert.True(reservation.IsCancelled);
        Assert.Equal(2, events.Count);
        Assert.Equal(2, events[^1].Version);
    }

    public async Task DisposeAsync()
    {
        store?.Dispose();
        await postgres.DisposeAsync();
    }
}
