using System.Timers;
using TransitApi.Api.Dtos.Trip.Response;
using TransitApi.Api.Interfaces;
using TransitApi.Api.Models.Trip;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TransitApi.Api.Services;

public class TripService : ITripService
{
    private readonly HttpClient _httpClient;

    public TripService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://journeyplanner.integration.sl.se/");
    }

    public async Task<List<LocationResponseDto>> FindStopsAsync(string query, CancellationToken cancellationToken = default)
    {
        var url = $"v3/stop-finder?name_sf={Uri.EscapeDataString(query)}&type_sf=any&any_obj_filter_sf=2";

        var response = await _httpClient.GetFromJsonAsync<StopFinderResponseDto>(url, cancellationToken);

        if (response?.Locations == null) return [];

        // Sortera automatiskt så att den bästa (isBest + högst matchQuality) hamnar först!
        return response.Locations
            .OrderByDescending(l => l.IsBest)
            .ThenByDescending(l => l.MatchQuality)
            .ToList();
    }

    // Denna kan du använda om du snabbt bara vill ha det enda "bästa" resmålet direkt
    public async Task<LocationResponseDto?> FindBestStopAsync(string query, CancellationToken cancellationToken = default)
    {
        var locations = await FindStopsAsync(query, cancellationToken);
        return locations.FirstOrDefault(); // Eftersom listan redan är sorterad är ettan alltid bäst!
    }

    public async Task<List<TripResultModel>> GetTripsAsync(string originId, string destinationId, string? date = null, string? time = null, CancellationToken cancellationToken = default)
    {
        var swedishTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, swedishTimeZone);

        if (string.IsNullOrEmpty(time) && string.IsNullOrEmpty(date))
        {
            localNow = localNow.AddMinutes(10);
        }

        var targetDate = string.IsNullOrEmpty(date) ? localNow.ToString("yyyyMMdd") : date;
        var targetTime = string.IsNullOrEmpty(time) ? localNow.ToString("HHmm") : time;
        var url = $"v3/trips?language=sv&type_origin=any&name_origin={originId}&type_destination=any&name_destination={destinationId}&itd_date={targetDate}&itd_time={targetTime}&calc_number_of_trips=3&type_via=any";

        var response = await _httpClient.GetFromJsonAsync<TripResponseDto>(url, cancellationToken);

        if (response?.Journeys == null) return [];

        return response.Journeys.Select(j => new TripResultModel
        {
            OriginName = j.Legs.FirstOrDefault()?.Origin.Name ?? "Okänd",
            DestinationName = j.Legs.LastOrDefault()?.Destination.Name ?? "Okänd",
            TotalDurationMinutes = j.TripDuration / 60,
            RealtimeDurationMinutes = j.TripRtDuration / 60,
            Interchanges = j.Interchanges,
            Legs = j.Legs.Where(l => !(string.IsNullOrEmpty(l.Transportation?.Name) && l.Origin.Name == l.Destination.Name)).Select(l =>
                {
                    var depUtc = l.Origin.DepartureTimeEstimated ?? l.Origin.DepartureTimePlanned ?? DateTime.MinValue;
                    var arrUtc = l.Destination.ArrivalTimeEstimated ?? l.Destination.ArrivalTimePlanned ?? DateTime.MinValue;

                    var depLocal = depUtc == DateTime.MinValue ? DateTime.MinValue : TimeZoneInfo.ConvertTimeFromUtc(depUtc, swedishTimeZone);
                    var arrLocal = arrUtc == DateTime.MinValue ? DateTime.MinValue : TimeZoneInfo.ConvertTimeFromUtc(arrUtc, swedishTimeZone);

                    return new TripLegModel
                    {
                        LineName = l.Transportation?.Name ?? "Gång / Okänt",
                        FromStation = l.Origin.Name,
                        ToStation = l.Destination.Name,
                        DepartureTime = depLocal,
                        ArrivalTime = arrLocal,
                        IsRealtime = l.Origin.DepartureTimeEstimated.HasValue
                    };
                }).ToList()
        }).ToList();
    }
}