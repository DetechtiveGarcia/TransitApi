using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TransitApi.Api.Services;

namespace TransitApi.Api.Endpoints;

public static class AudioEndpoints
{
    public static void MapAudioEndpoints(this WebApplication app)
    {
        app.MapPost("/api/audio/transcribe", async (
            IFormFile file,
            HttpClient http,
            IConfiguration config,
            AiService aiService // Skicka in vår nya AI-tjänst här!
        ) =>
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest(new { error = "No audio file received" });

            var apiKey = config["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return Results.Problem("Missing OpenAI Key in configuration.");

            try
            {
                // 1. SKICKA TILL WHISPER FÖR TRANSKRIBERING
                using var form = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/m4a");

                form.Add(fileContent, "file", "audio.m4a");
                form.Add(new StringContent("whisper-1"), "model");

                var whisperRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
                whisperRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                whisperRequest.Content = form;

                var whisperResponse = await http.SendAsync(whisperRequest);
                var whisperJson = await whisperResponse.Content.ReadAsStringAsync();

                if (!whisperResponse.IsSuccessStatusCode)
                {
                    return Results.Problem($"Whisper API Error: {whisperJson}");
                }

                using var doc = JsonDocument.Parse(whisperJson);
                var transcribedText = doc.RootElement.GetProperty("text").GetString();

                if (string.IsNullOrWhiteSpace(transcribedText))
                {
                    return Results.Ok(new { answer = "Jag hörde inte riktigt vad du sa, kan du försöka igen?" });
                }

                // Logga i konsolen så du ser vad du sa under utveckling
                Console.WriteLine($"[Whisper Transkribering]: {transcribedText}");

                // 2. SKICKA TEXTEN DIREKT TILL AI ROUTERN (SL-VERKTYGEN)
                var finalAiAnswer = await aiService.ProcessUserMessageAsync(transcribedText);

                // 3. SKICKA TILLBAKA SVARET TILL EXPO
                // Vi ändrar nyckeln till "text" här så att din frontend-kod (result.text) fortsätter fungera utan ändringar!
                return Results.Ok(new { text = finalAiAnswer });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Internal Server Error: {ex.Message}");
            }
        })
        .DisableAntiforgery();
    }
}