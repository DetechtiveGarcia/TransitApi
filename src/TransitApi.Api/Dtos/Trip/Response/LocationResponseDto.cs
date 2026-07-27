using System.Text.Json.Serialization;

namespace TransitApi.Api.Dtos.Trip.Response;

public class LocationResponseDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("disassembledName")]
    public string DisassembledName { get; set; } = "";

    [JsonPropertyName("isBest")]
    public bool IsBest { get; set; }

    [JsonPropertyName("matchQuality")]
    public int MatchQuality { get; set; }
}