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
            AudioUploadRequest request, // Ändrad parameter
            HttpClient http,
            IConfiguration config,
            AiService aiService
        ) =>
                {
                    Console.WriteLine("DEBUG: Base64-anrop mottaget!");

                    try
                    {
                        // 1. Konvertera Base64 tillbaka till bytes
                        byte[] fileBytes = Convert.FromBase64String(request.AudioBase64);

                        // 2. Skicka till Whisper
                        using var form = new MultipartFormDataContent();
                        var fileContent = new ByteArrayContent(fileBytes);
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/m4a");
                        form.Add(fileContent, "file", "audio.m4a");
                        form.Add(new StringContent("whisper-1"), "model");

                        var whisperRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
                        whisperRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config["OpenAI:ApiKey"]);
                        whisperRequest.Content = form;

                        var whisperResponse = await http.SendAsync(whisperRequest);
                        var whisperJson = await whisperResponse.Content.ReadAsStringAsync();

                        using var doc = JsonDocument.Parse(whisperJson);
                        var text = doc.RootElement.GetProperty("text").GetString();

                        var finalAiAnswer = await aiService.ProcessUserMessageAsync(text ?? "");
                        return Results.Ok(new { text = finalAiAnswer });
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