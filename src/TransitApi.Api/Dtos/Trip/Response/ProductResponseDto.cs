using System.Text.Json.Serialization;

namespace TransitApi.Api.Dtos.Trip.Response;

public class ProductResponseDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}