using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TransitApi.Api.Ai;
using TransitApi.Api.Services;
using Microsoft.Extensions.Options;
using TransitApi.Api.OpenAi;
using Microsoft.AspNetCore.Mvc;

namespace TransitApi.Api.Endpoints;

public static class AiEndpoints
{
    public static void MapAiEndpoints(this WebApplication app)
    {


        app.MapPost("/api/ai/ask", async (AiRequest request, AiService aiService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return Results.BadRequest("Message cannot be empty");

            var answer = await aiService.ProcessUserMessageAsync(request.Message);
            return Results.Ok(new { answer });
        });
    }

    private static async Task<JsonElement> CallOpenAi(
        HttpClient http,
        List<object> messages,
        object[]? tools,
        string apiKey)
    {
        var requestBody = new
        {
            model = "gpt-4o-mini",
            messages,
            tools,
            tool_choice = tools is null ? null : "auto"
        };

        var req = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/chat/completions"
        );

        req.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        req.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var res = await http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        Console.WriteLine("STATUS: " + res.StatusCode);
        Console.WriteLine("OPENAI RAW RESPONSE:");
        Console.WriteLine(json);


        if (!res.IsSuccessStatusCode)
        {
            var errorBody = await res.Content.ReadAsStringAsync();

            Console.WriteLine("OPENAI ERROR BODY:");
            Console.WriteLine(errorBody);

            throw new Exception($"OpenAI failed: {(int)res.StatusCode}");
        }

        return JsonDocument.Parse(json).RootElement;
    }

    // 🧩 TOOL HANDLERS (kopplar AI → din backend)

    private static async Task<object> HandleNext(SlService sl, JsonDocument args)
    {
        var query = args.RootElement.GetProperty("query").GetString()!;
        var line = args.RootElement.GetProperty("line").GetInt32();

        var site = (await sl.SearchSites(query)).FirstOrDefault();
        if (site is null) return new { error = "site not found" };

        var departures = await sl.GetDepartures(site.Id);

        SlDeparture? next = departures
            .Where(d => d.LineId == line)
            .OrderBy(d => d.Expected)
            .FirstOrDefault();

        return next is null
            ? new { error = "no departures" }
            : next;
    }

    private static async Task<object> HandleDepartures(SlService sl, JsonDocument args)
    {
        var query = args.RootElement.GetProperty("query").GetString()!;

        var site = (await sl.SearchSites(query)).FirstOrDefault();
        if (site is null) return new { error = "site not found" };

        return await sl.GetDepartures(site.Id);
    }

    private static async Task<object> HandleSearch(SlService sl, JsonDocument args)
    {
        var query = args.RootElement.GetProperty("query").GetString()!;
        return await sl.SearchSites(query);
    }
}

public class AiRequest
{
    public string Message { get; set; } = "";
}