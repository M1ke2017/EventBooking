using EventBooking.Application.Resources;
using Microsoft.AspNetCore.Mvc;
using EventBooking.Application.Reservations;
namespace EventBooking.Api.Endpoints;


public static class ResourceEndpoints
{
    public static void MapResourceEndpoints(this WebApplication app)
    {
        var resources = app.MapGroup("/api/resources");

        resources.MapGet("/", async ([FromServices] IResourceService service, CancellationToken ct) => Results.Ok(await service.GetAllAsync(ct)));

        resources.MapGet("/{id:guid}", async (Guid id, [FromServices] IResourceService service, CancellationToken ct) =>
        { var resource = await service.GetByIdAsync(id, ct); return resource is null ? Results.NotFound() : Results.Ok(resource); });

        resources.MapPost("/", async (ResourceRequest request, [FromServices] IResourceService service, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.Capacity <= 0) return Results.BadRequest("Invalid resource data.");
            var resource = await service.CreateAsync(request.Name, request.Capacity, ct);
            return Results.Created($"/api/resources/{resource.Id}", resource);
        });

        resources.MapPut("/{id:guid}", async (Guid id, ResourceRequest request, [FromServices] IResourceService service, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace (request.Name) || request.Capacity <= 0) return Results.BadRequest("Invalid resource data.");
            var updated = await service.UpdateAsync(id, request.Name, request.Capacity, ct);

            return updated ? Results.NoContent() : Results.NotFound();
        });

        resources.MapDelete("/{id:guid}", async (Guid id, [FromServices] IResourceService service, CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        });


        resources.MapGet("/{id:guid}/reservations", async (Guid id, DateTimeOffset from, DateTimeOffset to, IReservationCalendarReader reader, CancellationToken ct) =>
        {
            if (from >= to)return Results.BadRequest( new { error = "Invalid date range." });
            var reservations = await reader.GetAsync(id, from, to, ct);
            return Results.Ok(reservations);
        });

    }
}

public record ResourceRequest(string Name, int Capacity);
