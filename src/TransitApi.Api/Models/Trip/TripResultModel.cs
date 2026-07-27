namespace TransitApi.Api.Models.Trip;

public class TripResultModel
{
    public string OriginName { get; set; } = "";
    public string DestinationName { get; set; } = "";
    public int TotalDurationMinutes { get; set; }
    public int RealtimeDurationMinutes { get; set; }
    public int Interchanges { get; set; }
    public List<TripLegModel> Legs { get; set; } = [];
}
