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
        var transportQuery = string.IsNullOrEmpty(transportType) ? "" : $"&transport={transportType.ToUpper()}";
        var url = $"https://transport.integration.sl.se/v1/sites/{siteId}/departures?{transportQuery}";

        var json = await _http.GetFromJsonAsync<JsonElement>(url);

        var departures = json
            .GetProperty("departures")
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
    public string Destination { get; set; } = "";
    public string Direction { get; set; } = "";
    public string Display { get; set; } = "";
    public DateTime Expected { get; set; }
    public int LineId { get; set; }
}