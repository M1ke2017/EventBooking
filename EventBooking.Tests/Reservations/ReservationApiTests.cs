using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventBooking.Domain.Reservations;
using Marten;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using NpgsqlTypes;
using Testcontainers.PostgreSql;
using Xunit;

namespace EventBooking.Tests.Reservations;

public class ReservationApiTests : IClassFixture<ReservationApiFixture>
{
    private readonly HttpClient client;
    private readonly ReservationApiFixture fixture;

    public ReservationApiTests(ReservationApiFixture fixture)
    {
        this.fixture = fixture;
        client = fixture.Client;
    }

    [Fact]
    public async Task CreateAndGet_ShouldReturnReservation()
    {
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using var response = await CreateAsync(
            resourceId, userId, TestDate(10), TestDate(11));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content
            .ReadFromJsonAsync<CreatedResult>();

        Assert.NotNull(created);

        using var getResponse = await client.GetAsync(
            $"/api/reservations/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var reservation = await getResponse.Content
            .ReadFromJsonAsync<ReservationResult>();

        Assert.NotNull(reservation);
        Assert.Equal(resourceId, reservation.ResourceId);
        Assert.Equal(userId, reservation.UserId);
        Assert.False(reservation.IsCancelled);
    }

    [Fact]
    public async Task InvalidDateRange_ShouldReturn400()
    {
        using var response = await CreateAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TestDate(12),
            TestDate(10));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingReservation_ShouldReturn404()
    {
        using var response = await client.GetAsync(
            $"/api/reservations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OverlappingReservations_ShouldReturn409()
    {
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        using var first = await CreateAsync(
            resourceId, userId, TestDate(10), TestDate(12));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        using var second = await CreateAsync(
            resourceId, userId, TestDate(11), TestDate(13));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CancelReservation_ShouldReturn204AndThen409()
    {
        using var response = await CreateAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TestDate(14),
            TestDate(15));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content
            .ReadFromJsonAsync<CreatedResult>();

        Assert.NotNull(created);

        var url = $"/api/reservations/{created.Id}/cancel";

        using var cancelled = await client.PostAsync(url, null);

        Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);

        using var repeated = await client.PostAsync(url, null);

        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
    }

    [Fact]
    public async Task ReservationLifecycle_ShouldUpdateCalendar()
    {
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var start = TestDate(9);
        var end = TestDate(10);

        using var createdResponse = await CreateAsync(
            resourceId, userId, start, end);

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);

        var created = await createdResponse.Content
            .ReadFromJsonAsync<CreatedResult>();

        Assert.NotNull(created);

        var calendarUrl =
            $"/api/resources/{resourceId}/reservations" +
            $"?from={Uri.EscapeDataString(start.ToString("O"))}" +
            $"&to={Uri.EscapeDataString(end.ToString("O"))}";

        var periods = await client.GetFromJsonAsync<
            List<CalendarPeriodResult>>(calendarUrl);

        Assert.NotNull(periods);

        Assert.Contains(periods, p =>
            p.ReservationId == created.Id && !p.IsCancelled);

        using var cancelled = await client.PostAsync(
            $"/api/reservations/{created.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);

        periods = await client.GetFromJsonAsync<
            List<CalendarPeriodResult>>(calendarUrl);

        Assert.NotNull(periods);

        Assert.DoesNotContain(periods, p =>
            p.ReservationId == created.Id && !p.IsCancelled);

        using var newResponse = await CreateAsync(
            resourceId, Guid.NewGuid(), start, end);

        Assert.Equal(HttpStatusCode.Created, newResponse.StatusCode);
    }



    [Fact]
    public async Task ConcurrentReservations_ShouldAllowOnlyOne()
    {
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var start = TestDate(16);
        var end = TestDate(17);

        // Dwie równoczesne próby rezerwacji.
        var firstTask = CreateAsync(
            resourceId, userId, start, end);

        var secondTask = CreateAsync(
            resourceId, userId, start, end);

        var responses = await Task.WhenAll(
            firstTask, secondTask);

        using var first = responses[0];
        using var second = responses[1];

        var statuses = responses
            .Select(r => r.StatusCode)
            .ToArray();

        // Jedna rezerwacja musi zostać utworzona.
        Assert.Equal(1,
            statuses.Count(s => s == HttpStatusCode.Created));

        // Druga próba musi zostać odrzucona.
        Assert.Equal(1,
            statuses.Count(s => s == HttpStatusCode.Conflict));

        // PostgreSQL musi zawierać dokładnie jeden zajęty termin.
        var occupied = await fixture.CountOccupancyAsync(
            resourceId, start, end);

        Assert.Equal(1, occupied);
    }



    private Task<HttpResponseMessage> CreateAsync(
        Guid resourceId,
        Guid userId,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        return client.PostAsJsonAsync("/api/reservations", new
        {
            resourceId,
            userId,
            start,
            end
        });
    }

    private static DateTimeOffset TestDate(int hour)
    {
        return new DateTimeOffset(
            2030, 6, 15, hour, 0, 0, TimeSpan.FromHours(1));
    }

    private record CreatedResult(Guid Id);

    private record ReservationResult(
        Guid Id,
        Guid ResourceId,
        Guid UserId,
        DateTimeOffset Start,
        DateTimeOffset End,
        bool IsCancelled);

    private record CalendarPeriodResult(
        Guid ReservationId,
        Guid ResourceId,
        Guid UserId,
        DateTimeOffset Start,
        DateTimeOffset End,
        bool IsCancelled);
}

public class ReservationApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres =
        new PostgreSqlBuilder("postgres:17").Build();

    private WebApplicationFactory<Program>? factory;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();

        var connectionString = postgres.GetConnectionString();

        factory = new ReservationTestFactory(connectionString);

        Client = factory.CreateClient();

        var configuration = factory.Services
            .GetRequiredService<IConfiguration>();

        if (configuration.GetConnectionString("EventBooking")
            != connectionString)
        {
            throw new InvalidOperationException(
                "API nie otrzymało połączenia Testcontainers.");
        }

        var documentStore = factory.Services
            .GetRequiredService<IDocumentStore>();

        await documentStore.Storage
            .ApplyAllConfiguredChangesToDatabaseAsync();

        await using var connection = new NpgsqlConnection(
            connectionString);

        await connection.OpenAsync();

        const string createProjectionTable = """
            CREATE TABLE IF NOT EXISTS public.mt_doc_reservation
            (
                id uuid NOT NULL PRIMARY KEY,
                data jsonb NOT NULL,
                mt_last_modified timestamptz NOT NULL
                    DEFAULT transaction_timestamp(),
                mt_dotnet_type varchar NULL,
                mt_version bigint NOT NULL DEFAULT 0
            );
            """;

        await using (var setup = new NpgsqlCommand(
            createProjectionTable, connection))
        {
            await setup.ExecuteNonQueryAsync();
        }

        var scriptPath = Path.Combine(
            AppContext.BaseDirectory,
            "Database",
            "012_ReservationConcurrency.sql");

        var sql = await File.ReadAllTextAsync(scriptPath);

        await using (var command = new NpgsqlCommand(sql, connection))
        {
            await command.ExecuteNonQueryAsync();
        }

        await VerifyMartenConnectionAsync(
            documentStore, connection);
    }

    private static async Task VerifyMartenConnectionAsync(
        IDocumentStore store,
        NpgsqlConnection connection)
    {
        var id = Guid.NewGuid();

        var data = JsonSerializer.Serialize(new
        {
            Id = id,
            ResourceId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Start = "2035-01-01T10:00:00+00:00",
            End = "2035-01-01T11:00:00+00:00",
            IsCancelled = true
        });

        const string insert = """
            INSERT INTO public.mt_doc_reservation (id, data)
            VALUES (@id, @data);
            """;

        await using (var command = new NpgsqlCommand(
            insert, connection))
        {
            command.Parameters.AddWithValue("id", id);
            command.Parameters.Add("data", NpgsqlDbType.Jsonb)
                .Value = data;

            await command.ExecuteNonQueryAsync();
        }

        try
        {
            await using var session = store.QuerySession();

            var reservation = await session
                .LoadAsync<Reservation>(id);

            if (reservation?.Id != id)
            {
                throw new InvalidOperationException(
                    "Marten nie korzysta z bazy Testcontainers.");
            }
        }
        finally
        {
            const string delete = """
                DELETE FROM public.mt_doc_reservation
                WHERE id = @id;
                """;

            await using var command = new NpgsqlCommand(
                delete, connection);

            command.Parameters.AddWithValue("id", id);

            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task<string> InspectDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(
            postgres.GetConnectionString());

        await connection.OpenAsync();

        const string sql = """
            SELECT
                (SELECT COUNT(*)
                 FROM public.mt_events) AS events,

                (SELECT COUNT(*)
                 FROM public.mt_doc_reservation) AS projections,

                (SELECT COUNT(*)
                 FROM public.reservation_occupancy) AS occupancy,

                (SELECT COUNT(*)
                 FROM pg_trigger
                 WHERE tgname = 'trg_reservation_occupancy'
                   AND tgenabled <> 'D') AS triggers;
            """;

        await using var command = new NpgsqlCommand(
            sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        await reader.ReadAsync();

        var events = reader.GetInt64(0);
        var projections = reader.GetInt64(1);
        var occupancy = reader.GetInt64(2);
        var triggers = reader.GetInt64(3);

        return
            $"Events={events}, " +
            $"Projections={projections}, " +
            $"Occupancy={occupancy}, " +
            $"Trigger={triggers}";
    }

    public async Task<long> CountOccupancyAsync(
        Guid resourceId,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        await using var connection = new NpgsqlConnection(
            postgres.GetConnectionString());

        await connection.OpenAsync();

        const string sql = """
            SELECT COUNT(*)
            FROM public.reservation_occupancy
            WHERE resource_id = @resourceId
              AND reserved_period &&
                  tstzrange(@start, @end, '[)');
            """;

        await using var command = new NpgsqlCommand(
            sql, connection);

        command.Parameters.AddWithValue(
            "resourceId", resourceId);

        command.Parameters.AddWithValue(
            "start", start.ToUniversalTime());

        command.Parameters.AddWithValue(
            "end", end.ToUniversalTime());

        return (long)(await command.ExecuteScalarAsync())!;
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        factory?.Dispose();

        await postgres.DisposeAsync();
    }
}

public class ReservationTestFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EventBooking"] =
                        connectionString
                });
        });

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:EventBooking"] =
                        connectionString
                });
        });

        return base.CreateHost(builder);
    }
}
