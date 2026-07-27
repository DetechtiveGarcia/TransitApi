using System.Text.Json.Serialization;

namespace TransitApi.Api.Dtos.Trip.Response;

public class JourneyResponseDto
{
    [JsonPropertyName("tripDuration")]
    public int TripDuration { get; set; }

    [JsonPropertyName("tripRtDuration")]
    public int TripRtDuration { get; set; }

    [JsonPropertyName("interchanges")]
    public int Interchanges { get; set; }

    [JsonPropertyName("legs")]
    public List<LegResponseDto> Legs { get; set; } = [];
}
