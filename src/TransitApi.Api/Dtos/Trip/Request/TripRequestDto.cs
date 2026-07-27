namespace TransitApi.Api.Dtos.Trip.Request;

public class TripRequestDto
{
    public string OriginId { get; set; } = "";      // T.ex. "9091001000009117"
    public string DestinationId { get; set; } = ""; // T.ex. "9091001000009294"
    public string Language { get; set; } = "sv";
    public int CalcNumberOfTrips { get; set; } = 3; // Ofta bra att få flera alternativ än bara 1
    public DateTime? Date { get; set; }             // Om man vill söka för en specifik tidpunkt
}
