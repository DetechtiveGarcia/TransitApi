using System.Text.Json.Serialization;

namespace TransitApi.Api.Dtos.Trip.Response;

public class TransportationResponseDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("number")]
    public string Number { get; set; } = "";

    [JsonPropertyName("disassembledName")]
    public string DisassembledName { get; set; } = "";

    [JsonPropertyName("product")]
    public ProductResponseDto? Product { get; set; }
}