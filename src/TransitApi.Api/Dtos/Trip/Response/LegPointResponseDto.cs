using System.Text.Json.Serialization;

namespace TransitApi.Api.Dtos.Trip.Response;

public class LegPointResponseDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("departureTimePlanned")]
    public DateTime? DepartureTimePlanned { get; set; }

    [JsonPropertyName("departureTimeEstimated")]
    public DateTime? DepartureTimeEstimated { get; set; }

    [JsonPropertyName("arrivalTimePlanned")]
    public DateTime? ArrivalTimePlanned { get; set; }

    [JsonPropertyName("arrivalTimeEstimated")]
    public DateTime? ArrivalTimeEstimated { get; set; }
}