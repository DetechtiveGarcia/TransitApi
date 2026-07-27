using System.ComponentModel;
using System.Text.Json;
using TransitApi.Api.Dtos;
using TransitApi.Api.Models;

namespace TransitApi.Api.Services;

public class SlService
{
    private readonly HttpClient _http;

    public SlService(HttpClient http) => _http = http;

    public async Task<List<SlDeparture>> GetDepartures(int siteId, string? transportType = null)
    {

        var url = $"https://transport.integration.sl.se/v1/sites/{siteId}/departures";

        if (!string.IsNullOrEmpty(transportType))
        {
            url += $"?&transport={transportType.ToUpper()}";
        }


        Console.WriteLine($"DEBUG: Anropar SL med URL: {url}");

        try
        {


            var json = await _http.GetFromJsonAsync<JsonElement>(url);

            var departuresJson = json.GetProperty("departures");
            Console.WriteLine($"DEBUG: Antal element i JSON: {departuresJson.GetArrayLength()}");

            var departures = departuresJson
                .EnumerateArray()
                .Select(d => new SlDeparture
                {
                    Destination = d.GetProperty("destination").GetString() ?? "",
                    Direction = d.GetProperty("direction").GetString() ?? "", 
                    Display = d.GetProperty("display").GetString() ?? "",
                    Expected = d.GetProperty("expected").GetDateTime(),
                    LineId = d.GetProperty("line").GetProperty("id").GetInt32()
                })
                .ToList();

            return departures;


        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"API FEL: {ex.Message}");
            return [];
        }
    }

    public async Task<List<SiteDto>> SearchSites
        (string query)
    {
            var url = "https://transport.integration.sl.se/v1/sites";

            var sites = await _http.GetFromJsonAsync<List<SlSite>>(url);

        if (sites is null || sites.Count <= 0)
        {
            Console.WriteLine("Inga sites hittades");
            return [];
        }

        foreach (var site in sites)
        {
            Console.WriteLine("SITES: " + site.Name);
        }

        Console.WriteLine("SITES: " + sites.ToList());

        return sites
        .OrderByDescending(s => s.Name.Equals(query, StringComparison.OrdinalIgnoreCase))
        .ThenByDescending(s => s.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        .Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
        .Take(5)
        .Select(s => new SiteDto
        {
            Id = s.Id,
            Name = s.Name,
            Lat = s.Lat,
            Lon = s.Lon
        })
        .ToList();
    }
}

public class SlDeparture
{
    public string Destination { get; set; } = "";
    public string Direction { get; set; } = "";
    public string Display { get; set; } = "";
    public DateTime Expected { get; set; }
    public int LineId { get; set; }
}