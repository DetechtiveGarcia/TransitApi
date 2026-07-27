using System.Text.Json.Serialization;

namespace TransitApi.Api.Dtos.Trip.Response;

public class LegResponseDto
{
    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("origin")]
    public LegPointResponseDto Origin { get; set; } = new();

    [JsonPropertyName("destination")]
    public LegPointResponseDto Destination { get; set; } = new();

    [JsonPropertyName("transportation")]
    public TransportationResponseDto? Transportation { get; set; }
}
