namespace TransitApi.Api.Dtos;

public class DepartureDto
{
    public int Line { get; set; }
    public string Destination { get; set; } = null!;
    public string DepartureIn { get; set; } = null!;
    public DateTime Expected { get; set; }
}