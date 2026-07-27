namespace TransitApi.Api.Dtos.Trip.Response;

using System.Text.Json.Serialization;

public class StopFinderResponseDto
{
    [JsonPropertyName("locations")]
    public List<LocationResponseDto> Locations { get; set; } = [];
}