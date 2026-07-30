using System.Net.Http.Headers;
using System.Text.Json;
using TransitApi.Api.Services;

namespace TransitApi.Api;

public class AudioService
{
    private readonly HttpClient _http;
    private readonly AiService _aiService;
    private readonly string _apiKey;

    public AudioService(HttpClient http, AiService aiService, IConfiguration config)
    {
        _http = http;
        _aiService = aiService;
        _apiKey = config["OpenAI:ApiKey"] ?? throw new Exception("Missing OpenAI API key");
    }

    public async Task<object> ProcessAudioAndChatAsync(string audioBase64)
    {
        // 1. Konvertera Base64 till bytes
        byte[] fileBytes = Convert.FromBase64String(audioBase64);

        // 2. Skicka till OpenAI Whisper
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/m4a");
        form.Add(fileContent, "file", "audio.m4a");
        form.Add(new StringContent("whisper-1"), "model");

        var whisperRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
        whisperRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        whisperRequest.Content = form;

        var whisperResponse = await _http.SendAsync(whisperRequest);

        if (!whisperResponse.IsSuccessStatusCode)
        {
            var errorJson = await whisperResponse.Content.ReadAsStringAsync();
            throw new Exception($"Whisper failed: {(int)whisperResponse.StatusCode} - {errorJson}");
        }

        var whisperJson = await whisperResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(whisperJson);
        var transcribedText = doc.RootElement.GetProperty("text").GetString() ?? string.Empty;

        // 3. Skicka transkriberingen vidare till AiService för SL-svar
        var finalAiAnswer = await _aiService.ProcessUserMessageAsync(transcribedText);

        // Returnera både det transkriberade (så frontenden kan logga det) och AI-svaret
        return new
        {
            transcribedText = transcribedText,
            text = finalAiAnswer
        };
    }
}