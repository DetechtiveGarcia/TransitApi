namespace TransitApi.Api.Dtos.Trip.Request;

public class StopFinderRequestDto
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "any";
    public int AnyObjFilter { get; set; } = 2;
}