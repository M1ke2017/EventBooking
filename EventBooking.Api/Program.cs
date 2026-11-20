using EventBooking.Api.Configuration;
using EventBooking.Api.Endpoints;
using EventBooking.Application.Reservations;
using EventBooking.Application.Resources;
using EventBooking.Domain.Reservations;
using EventBooking.Infrastructure.Reservations;
using EventBooking.Infrastructure.Resources;
using JasperFx.Events.Projections;
using Marten;
using Marten.Events.Projections;

var builder = WebApplication.CreateBuilder(args);

// Marten + PostgreSQL
builder.Services.AddMarten(options =>
{
    options.Schema.For<Reservation>();

    options.Projections.Snapshot<Reservation>(
        SnapshotLifecycle.Inline);
});

// Konfiguracja połączenia pobierana przez DI
builder.Services.AddSingleton<
    IConfigureMarten,
    EventBookingMartenConfiguration>();

// Reservations
builder.Services.AddScoped<IReservationEventStore, MartenReservationEventStore>();
builder.Services.AddScoped<IReservationReader, MartenReservationReader>();
builder.Services.AddScoped<IReservationHistoryReader, MartenReservationHistoryReader>();
builder.Services.AddScoped<IReservationCalendarReader, MartenReservationCalendarReader>();
builder.Services.AddScoped<IReservationConflictChecker, MartenReservationConflictChecker>();

// Handlers
builder.Services.AddScoped<CreateReservationHandler>();
builder.Services.AddScoped<CancelReservationHandler>();
builder.Services.AddScoped<RescheduleReservationHandler>();
builder.Services.AddScoped<ChangeReservationResourceHandler>();

// Resources
builder.Services.AddScoped<IResourceService, MartenResourceService>();

var app = builder.Build();

app.MapGet("/", () => "EventBooking API");
app.MapReservationEndpoints();
app.MapResourceEndpoints();

app.Run();

public partial class Program { }
