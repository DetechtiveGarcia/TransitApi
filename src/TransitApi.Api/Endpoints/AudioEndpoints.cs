using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TransitApi.Api.Services;

namespace TransitApi.Api.Endpoints;

public record AudioUploadRequest(string AudioBase64);

public static class AudioEndpoints
{
    public static void MapAudioEndpoints(this WebApplication app)
    {
        app.MapPost("/api/audio/transcribe", async (
            AudioUploadRequest request,
            AudioService audioService
        ) =>
        {
            Console.WriteLine("DEBUG: Base64-anrop mottaget!");

            try
            {
                var result = await audioService.ProcessAudioAndChatAsync(request.AudioBase64);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return Results.Problem(ex.Message);
            }
        })
        .DisableAntiforgery();
    }
}