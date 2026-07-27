using TransitApi.Api.Dtos.Trip.Response;
using TransitApi.Api.Models.Trip;

namespace TransitApi.Api.Interfaces;

public interface ITripService
{
    Task<List<LocationResponseDto>> FindStopsAsync(string query, CancellationToken cancellationToken = default);
    Task<LocationResponseDto?> FindBestStopAsync(string query, CancellationToken cancellationToken = default);
    Task<List<TripResultModel>> GetTripsAsync(string originId, string destinationId, string? date = null, string? time = null, CancellationToken cancellationToken = default);
}
