namespace TransitApi.Api.Models.Trip;

public class TripLegModel
{
    public string LineName { get; set; } = ""; 
    public string FromStation { get; set; } = "";
    public string ToStation { get; set; } = "";
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public bool IsRealtime { get; set; }
}
