using EventBooking.Application.Reservations;
using Npgsql;

namespace EventBooking.Api.Endpoints;

public static class ReservationEndpoints
{
    public static void MapReservationEndpoints(this WebApplication app)
    {
        var reservations = app.MapGroup("/api/reservations");

        reservations.MapPost("/", async (
            CreateReservation command,
            CreateReservationHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var id = await handler.HandleAsync(command, ct);

                return Results.Created(
                    $"/api/reservations/{id}",
                    new { id });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ReservationConflictException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (Exception ex) when (IsOverlapConstraintViolation(ex))
            {
                return Results.Conflict(new
                {
                    error = "Zasób został właśnie zarezerwowany przez innego użytkownika."
                });
            }
        });

        reservations.MapGet("/{id:guid}", async (
            Guid id,
            IReservationReader reader,
            CancellationToken ct) =>
        {
            var reservation = await reader.GetByIdAsync(id, ct);

            return reservation is null
                ? Results.NotFound()
                : Results.Ok(reservation);
        });

        reservations.MapGet("/{id:guid}/history", async (
            Guid id,
            IReservationHistoryReader reader,
            CancellationToken ct) =>
        {
            var history = await reader.GetAsync(id, ct);

            return history.Count == 0
                ? Results.NotFound()
                : Results.Ok(history);
        });

        reservations.MapPost("/{id:guid}/cancel", async (
            Guid id,
            CancelReservationHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                await handler.HandleAsync(id, ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        });

        reservations.MapPut("/{id:guid}/reschedule", async (
            Guid id,
            RescheduleReservationRequest request,
            RescheduleReservationHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                await handler.HandleAsync(
                    id, request.Start, request.End, ct);

                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ReservationConflictException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (Exception ex) when (IsOverlapConstraintViolation(ex))
            {
                return Results.Conflict(new
                {
                    error = "Wybrany termin został właśnie zajęty."
                });
            }
        });

        reservations.MapPut("/{id:guid}/resource", async (
            Guid id,
            ChangeResourceRequest request,
            ChangeReservationResourceHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                await handler.HandleAsync(
                    id, request.ResourceId, ct);

                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ReservationConflictException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (Exception ex) when (IsOverlapConstraintViolation(ex))
            {
                return Results.Conflict(new
                {
                    error = "Zasób jest już zajęty w wybranym terminie."
                });
            }
        });
    }

    private static bool IsOverlapConstraintViolation(Exception exception)
    {
        Exception? current = exception;

        while (current is not null)
        {
            if (current is PostgresException postgres &&
                postgres.SqlState == PostgresErrorCodes.ExclusionViolation &&
                postgres.ConstraintName == "reservation_occupancy_no_overlap")
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}

public record RescheduleReservationRequest(
    DateTimeOffset Start,
    DateTimeOffset End);

public record ChangeResourceRequest(Guid ResourceId);
