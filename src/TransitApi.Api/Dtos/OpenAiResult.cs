using System.Text.Json;

namespace TransitApi.Api.Dtos;

public class OpenAiResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public JsonElement? Data { get; set; }
}