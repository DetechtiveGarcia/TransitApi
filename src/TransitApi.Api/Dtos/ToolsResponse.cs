namespace TransitApi.Api.Dtos;

public class ToolResponse
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Error { get; set; }
}