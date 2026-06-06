using System.ComponentModel;
using System.Text.Json;
using TransitApi.Api.Dtos;
using TransitApi.Api.Models;

namespace TransitApi.Api.Services;

public class SlService
{
    private readonly HttpClient _http;

    public SlService(HttpClient http) => _http = http;

    public async Task<List<SlDeparture>> GetDepartures(int siteId)
    {
        var url = $"https://transport.integration.sl.se/v1/sites/{siteId}/departures?transport=BUS";

        var json = await _http.GetFromJsonAsync<JsonElement>(url);

        var departures = json
            .GetProperty("departures")
            .EnumerateArray()
            .Select(d => new SlDeparture
            {
                LineId = d.GetProperty("line").GetProperty("id").GetInt32(),
                Destination = d.GetProperty("destination").GetString() ?? "",
                Display = d.GetProperty("display").GetString() ?? "",
                Expected = d.GetProperty("expected").GetDateTime()
            })
            .ToList();

        return departures;
    }

    public async Task<List<SiteDto>> SearchSites
        (string query)
    {
            var url = "https://transport.integration.sl.se/v1/sites";

            var sites = await _http.GetFromJsonAsync<List<SlSite>>(url);

            return sites?
                .Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .Select(s => new SiteDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Lat = s.Lat,
                    Lon = s.Lon
                })
                .ToList()
                ?? [];
    }
}

public class SlDeparture
{
    public int LineId { get; set; }
    public string Destination { get; set; } = "";
    public string Display { get; set; } = "";
    public DateTime Expected { get; set; }
}