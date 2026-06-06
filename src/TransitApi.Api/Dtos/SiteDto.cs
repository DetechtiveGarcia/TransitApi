namespace TransitApi.Api.Dtos;

public class SiteDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double Lat { get; set; }
    public double Lon { get; set; }
}