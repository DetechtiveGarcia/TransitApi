using Microsoft.AspNetCore.Http.HttpResults;
using TransitApi.Api.Dtos;
using TransitApi.Api.Interfaces;
using TransitApi.Api.Services;

namespace TransitApi.Api.Endpoints;

public static class TransitEndpoints
{
    public static void MapTransitEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/transit")
            .WithTags("Transit")
            .WithDescription("Real-time SL transit endpoints");

        // 1. Hämtar nästa avgångar för en station (med valfri filtrering på linje)
        group.MapGet("/departures", async (
            string query,
            int? line,
            SlService sl) =>
        {
            var site = (await sl.SearchSites(query)).FirstOrDefault();

            if (site is null)
                return Results.NotFound();

            var departures = await sl.GetDepartures(site.Id);

            if (departures is null || departures.Count == 0)
                return Results.NotFound();

            var result = departures
                .Where(d => line == null || d.LineId == line)
                .OrderBy(d => d.Expected)
                .Take(10)
                .Select(d => new DepartureDto
                {
                    Line = d.LineId,
                    Destination = d.Destination,
                    DepartureIn = d.Display,
                    Expected = d.Expected
                })
                .ToList();

            return Results.Ok(new
            {
                site = site.Name,
                count = result.Count,
                departures = result
            });
        });

        // 2. Stop Finder (Sök stationer på namn)
        group.MapGet("/search-stop", async (string query, ITripService tripService, CancellationToken ct) =>
        {
            var stops = await tripService.FindBestStopAsync(query, ct);
            return Results.Ok(stops);
        });

        // 3. Ruttplanerare (Hämtar resvägar med byten och tider)
        group.MapGet("/route", async (string originId, string destinationId, string? date, string? time, ITripService tripService, CancellationToken ct) =>
        {
            var trips = await tripService.GetTripsAsync(originId, destinationId, date, time, ct);
            return Results.Ok(trips);
        });
    }
}