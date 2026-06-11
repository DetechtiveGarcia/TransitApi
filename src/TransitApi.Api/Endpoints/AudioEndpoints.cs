using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TransitApi.Api.Endpoints;

public static class AudioEndpoints
{
    public static void MapAudioEndpoints(this WebApplication app)
    {
        app.MapPost("/api/audio/transcribe", async (
            IFormFile file,
            HttpClient http, // Inbakat via .NET Dependency Injection
            IConfiguration config
        ) =>
        {
            // 1. Validera att vi faktiskt fick en fil från mobilen
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "No audio file received" });
            }

            // 2. Hämta din API-nyckel från appsettings.json / User Secrets
            var apiKey = config["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Results.Problem("Missing OpenAI Key in configuration.");
            }

            try
            {
                // 3. Skapa multipart form-data som OpenAI kräver
                using var form = new MultipartFormDataContent();

                // Öppna strömmen från IFormFile
                using var fileStream = file.OpenReadStream();
                var fileContent = new StreamContent(fileStream);

                // OpenAI är petiga med Content-Type för ljudfiler
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/m4a");

                // "file" är parameternamnet som OpenAI Whisper förväntar sig
                form.Add(fileContent, "file", "audio.m4a");

                // Berätta för OpenAI vilken modell som ska användas
                form.Add(new StringContent("whisper-1"), "model");

                // 4. Bygg requesten till OpenAI
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.openai.com/v1/audio/transcriptions"
                );

                // Lägg till din Bearer token
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = form;

                // 5. Skicka till OpenAI
                var response = await http.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // Om OpenAI ger felmeddelande (t.ex. fel API-nyckel eller korrupt fil)
                    return Results.Problem($"OpenAI API Error: {json}");
                }

                // 6. Parsa svaret och plocka ut "text"
                using var doc = JsonDocument.Parse(json);
                var text = doc.RootElement.GetProperty("text").GetString();

                // Skicka tillbaka den faktiska transkriberingen till Expo-appen
                return Results.Ok(new { text });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Internal Server Error: {ex.Message}");
            }
        })
        .DisableAntiforgery(); // Håller dörren öppen för Expo Go
    }
}