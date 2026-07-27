namespace TransitApi.Api.Dtos.Trip.Response;

using System.Text.Json.Serialization;

public class TripResponseDto
{
    [JsonPropertyName("journeys")]
    public List<JourneyResponseDto> Journeys { get; set; } = [];
}