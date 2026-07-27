using System.Text.Json;
using TransitApi.Api.Services;
using TransitApi.Api.Interfaces;

namespace TransitApi.Api.Endpoints;

public static class AiEndpoints
{
    public static void MapAiEndpoints(this WebApplication app)
    {
        app.MapPost("/api/ai/ask", async (AiRequest request, AiService aiService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return Results.BadRequest("Message cannot be empty");

            var answer = await aiService.ProcessUserMessageAsync(request.Message);
            return Results.Ok(new { answer });
        });
    }

    // 🧩 TOOL HANDLERS (kopplar AI → din backend)

    public static async Task<object> ExecuteNextDepartureTool(SlService sl, JsonDocument args)
    {
        var query = args.RootElement.GetProperty("query").GetString()!;
        var line = args.RootElement.GetProperty("line").GetInt32();
        var destination = args.RootElement.TryGetProperty("destination", out var dest) ? dest.GetString() : null;

        var site = (await sl.SearchSites(query)).FirstOrDefault();
        if (site is null) return new { error = "site not found" };

        var departures = await sl.GetDepartures(site.Id);

        SlDeparture? next = departures
            .Where(d => d.LineId == line)
            .Where(d => string.IsNullOrEmpty(destination) ||
                d.Destination.Contains(destination, StringComparison.OrdinalIgnoreCase) ||
                d.Direction.Contains(destination, StringComparison.OrdinalIgnoreCase))
            .OrderBy(d => d.Expected)
            .FirstOrDefault();

        return next is null
            ? new
            {
                status = "no_match_found",
                message = $"Ingen avgång hittades för linje {line}.",
                available_departures = departures.OrderBy(d => d.Expected).Take(5).ToList()
            }
            : next;
    }

    public static async Task<object> HandleDepartures(SlService sl, JsonDocument args)
    {
        var query = args.RootElement.GetProperty("query").GetString()!;
        string? transport = args.RootElement.TryGetProperty("transport", out var t) ? t.GetString() : null;

        var site = (await sl.SearchSites(query)).FirstOrDefault();
        if (site is null) return new { error = "site not found" };

        return await sl.GetDepartures(site.Id, transport);
    }

    public static async Task<object> HandleSearch(ITripService tripService, JsonDocument args)
    {
        var query = args.RootElement.GetProperty("query").GetString()!;
        var bestStop = await tripService.FindBestStopAsync(query);

        return bestStop is null ? new { error = "stop not found" } : bestStop;
    }

    public static async Task<object> HandleRoute(ITripService tripService, JsonDocument args)
    {
        var originId = args.RootElement.GetProperty("originId").GetString()!;
        var destinationId = args.RootElement.GetProperty("destinationId").GetString()!;

        string? date = args.RootElement.TryGetProperty("date", out var d) ? d.GetString() : null;
        string? time = args.RootElement.TryGetProperty("time", out var t) ? t.GetString() : null;

        var trips = await tripService.GetTripsAsync(originId, destinationId, date, time, default);

        if (trips.Count == 0)
        {
            return new { status = "no_trips_found", message = "Inga resvägar kunde hittas mellan dessa stationer för angiven tid." };
        }

        return trips;
    }
}

public class AiRequest
{
    public string Message { get; set; } = "";
}