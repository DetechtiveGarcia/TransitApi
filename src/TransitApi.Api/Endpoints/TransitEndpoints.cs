using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json;
using TransitApi.Api.Dtos;
using TransitApi.Api.Services;

namespace TransitApi.Api.Endpoints;

public static class TransitEndpoints
{
    public static void MapTransitEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/transit")
            .WithTags("Transit")
            .WithDescription("Real-time SL transit endpoints");



        group.MapGet("/next", async (
            string query,
            int line,
            SlService sl) =>
                {
                    var site = (await sl.SearchSites(query)).FirstOrDefault();

                    if (site is null)
                        return Results.NotFound();

                    var departures = await sl.GetDepartures(site.Id);

                    var next = departures
                        .Where(d => d.LineId == line)
                        .OrderBy(d => d.Expected)
                        .FirstOrDefault();

                    if (next == null)
                        return Results.NotFound();

                    return Results.Ok(Map(next));
                });

        group.MapGet("/next/to", async (
            string query,
            string destination,
            SlService sl) =>
                {
                    var site = (await sl.SearchSites(query)).FirstOrDefault();

                    if (site is null)
                        return Results.NotFound();

                    var departures = await sl.GetDepartures(site.Id);

                    var next = departures
                        .Where(d =>
                            d.Destination.Equals(destination, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(d => d.Expected)
                        .FirstOrDefault();

                    if (next is null)
                        return Results.NotFound();

                    return Results.Ok(Map(next));
                });

        group.MapGet("/departures", async (
            string query,
            int? line,
            SlService sl) =>
                {
                    var site = (await sl.SearchSites(query)).FirstOrDefault();

                    if (site is null)
                        return Results.NotFound();

                    var departures = await sl.GetDepartures(site.Id);

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
                        count = departures.Count,
                        departures = result
                    });
        });

        group.MapGet("/sites/search", async (string query, SlService sl) =>
        {
            var sites = await sl.SearchSites(query);
            var site = ResolveBestSite(sites, query);

            if (site is null)
                return Results.NotFound("No matching stop found");

            return Results.Ok(site);
        });






    }

    private static SiteDto? ResolveBestSite(List<SiteDto> sites, string query)
    {
        query = query.Trim().ToLower();

        return sites
            .Select(site => new
            {
                Site = site,
                Score = GetScore(site.Name, query)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Site)
            .FirstOrDefault();
    }

    private static int GetScore(string name, string query)
    {
        name = name.ToLower();

        if (name == query)
            return 100;

        if (name.StartsWith(query))
            return 75;

        if (name.Contains(query))
            return 50;

        return 0;
    }

    private static DepartureDto Map(SlDeparture d) =>
    new()
    {
        Line = d.LineId,
        Destination = d.Destination,
        DepartureIn = d.Display,
        Expected = d.Expected
    };
}