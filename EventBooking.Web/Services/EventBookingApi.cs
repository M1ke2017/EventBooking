
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventBooking.Web.Services;

public class EventBookingApi(HttpClient http)
{
    public async Task<List<ResourceDto>> GetResourcesAsync()
    { return await http.GetFromJsonAsync<List<ResourceDto>>("api/resources") ?? []; }

    public async Task<ResourceDto> CreateResourceAsync(string name, int capacity)
    {
        var response = await http.PostAsJsonAsync("api/resources", new { name, capacity });
        await CheckAsync(response); return (await response.Content.ReadFromJsonAsync<ResourceDto>())!;
    }

    public async Task UpdateResourceAsync(Guid id, string name, int capacity)
    { var response = await http.PutAsJsonAsync($"api/resources/{id}", new { name, capacity }); await CheckAsync(response); }

    public async Task DeleteResourceAsync(Guid id)
    { var response = await http.DeleteAsync($"api/resources/{id}"); await CheckAsync(response); }

    public async Task<Guid> CreateReservationAsync(Guid resourceId, Guid userId, DateTimeOffset start, DateTimeOffset end)
    {
        var response = await http.PostAsJsonAsync("api/reservations", new { resourceId, userId, start, end }); await CheckAsync(response);
        var result = await response.Content.ReadFromJsonAsync<CreateReservationResult>(); return result!.Id;
    }

    public async Task<ReservationDto?> GetReservationAsync(Guid id)
    {
        var response = await http.GetAsync($"api/reservations/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        await CheckAsync(response); return await response.Content.ReadFromJsonAsync<ReservationDto>();
    }

    public async Task<List<HistoryDto>> GetHistoryAsync(Guid id)
    {
        var response = await http.GetAsync($"api/reservations/{id}/history");
        if (response.StatusCode == HttpStatusCode.NotFound)return [];
        await CheckAsync(response); return await response.Content.ReadFromJsonAsync<List<HistoryDto>>() ?? [];
    }

    public async Task CancelAsync(Guid id)
    { var response = await http.PostAsync($"api/reservations/{id}/cancel", null);await CheckAsync(response); }

    public async Task RescheduleAsync(Guid id, DateTimeOffset start, DateTimeOffset end)
    {
        var response = await http.PutAsJsonAsync($"api/reservations/{id}/reschedule", new { start, end });
        await CheckAsync(response);
    }

    public async Task ChangeResourceAsync(Guid id, Guid resourceId)
    {
        var response = await http.PutAsJsonAsync($"api/reservations/{id}/resource",new { resourceId });
        await CheckAsync(response);
    }

    private static async Task CheckAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)return;
        var message = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"HTTP {(int)response.StatusCode}: {message}");
    }

    public async Task<List<ReservedPeriodDto>> GetReservedPeriodsAsync(Guid resourceId, DateTimeOffset from, DateTimeOffset to)
    {
        var fromValue = Uri.EscapeDataString(from.ToString("O"));
        var toValue = Uri.EscapeDataString(to.ToString("O"));
        return await http.GetFromJsonAsync<List<ReservedPeriodDto>>($"api/resources/{resourceId}/reservations?from={fromValue}&to={toValue}") ?? [];
    }

}

public record ResourceDto(Guid Id, string Name, int Capacity);

public record CreateReservationResult(Guid Id);

public record ReservationDto(Guid Id, Guid ResourceId, Guid UserId, DateTimeOffset Start, DateTimeOffset End, bool IsCancelled);

public record HistoryDto(long Version, string EventType, DateTimeOffset Timestamp, JsonElement Data);

public record ReservedPeriodDto(
    Guid ReservationId,
    Guid ResourceId,
    Guid UserId,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsCancelled);