using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TransitApi.Api.Ai;
using TransitApi.Api.Endpoints;
using TransitApi.Api.Interfaces;
using TransitApi.Api.OpenAi;

namespace TransitApi.Api.Services;

public class AiService
{
    private readonly HttpClient _http;
    private readonly SlService _sl;
    private readonly ITripService _tripService;
    private readonly string _apiKey;

    public AiService(HttpClient http, SlService sl, ITripService tripService, IConfiguration config)
    {
        _http = http;
        _sl = sl;
        _tripService = tripService;
        _apiKey = config["OpenAI:ApiKey"] ?? throw new Exception("Missing OpenAI API key");
    }

    public async Task<string> ProcessUserMessageAsync(string userMessage)
    {
        var tools = AiTools.GetTools();

        var messages = new List<object>
        {
            new
            {
                role = "system",
                content =
                    """
                        INSTRUCTIONS:
                        You are a professional transit AI for Stockholm public transport (SL).
                        1. NEVER guess departure times. Always use tools.
                        2. If the user asks for a trip, route, or travel time between two places (from A to B), you MUST use the `get_route` tool with `originQuery` and `destinationQuery`.
                        3. If the user asks about a single station only, use `get_departures`.
                        4. LANGUAGE: Always respond in the same language as the user.
                        5. Correct Swedish: Use 'avgår' for departures.
                        6. Do not end responses with conversational filler questions

                        OTHER RULES:
                        - LANGUAGE: Always respond in the same language as the user.
                        - Correct Swedish: Use 'avgår' for buses/trains.

                        DISPLAY & FORMATTING RULES:
                        When presenting travel details or departures, format the response cleanly using Markdown for the screen:
                        1. Make the absolute next departure time and line number **bold**.
                        2. Structure the response so the primary information comes first, followed by a clean list of subsequent departures.
                        3. DEVIATIONS (Störningar): If there are any traffic disruptions, delays, or alerts from the SL tool, you MUST clearly highlight them at the top with a warning emoji (⚠️) so the user sees it immediately.

                        Example layout to follow:
                        **Buss 444** till Slussen avgår **14:36** från Orminge centrum.
                        - **Resa:** cirka 17 minuter (Ankomst 14:54)
                        - ⚠️ *Obs: 3 minuters försening på grund av köer.* (Endast om störningar finns)

                        **Kommande avgångar:**
                        - **14:50** – Buss 444
                        - **14:59** – Buss 445
                    """
            },
            new { role = "user", content = userMessage }
        };

        // 🔁 FIRST CALL
        var firstResponse = await CallOpenAi(messages, tools);
        var message = firstResponse.GetProperty("choices")[0].GetProperty("message");

        // 🧠 NO TOOLS → returnera direkt
        if (!message.TryGetProperty("tool_calls", out var toolCalls) ||
            toolCalls.ValueKind != JsonValueKind.Array ||
            toolCalls.GetArrayLength() == 0)
        {
            return message.GetProperty("content").GetString() ?? "Kunde inte generera ett svar.";
        }

        var assistantMessage = new
        {
            role = "assistant",
            content = message.GetProperty("content").GetString(),
            tool_calls = toolCalls
        };
        messages.Add(assistantMessage);

        // Hantera alla tool calls
        foreach (var toolCall in toolCalls.EnumerateArray())
        {
            var functionName = toolCall.GetProperty("function").GetProperty("name").GetString();
            var argsJson = toolCall.GetProperty("function").GetProperty("arguments").GetString();

            if (string.IsNullOrWhiteSpace(argsJson)) continue;

            using var args = JsonDocument.Parse(argsJson);

            object toolResult = functionName switch
            {
                "get_next_departure" => await AiEndpoints.ExecuteNextDepartureTool(_sl, args),

                "get_departures" => await AiEndpoints.HandleDepartures(_sl, args),


                "get_route" => await HandleGetRouteAsync(args.RootElement),

                _ => throw new Exception($"Unknown tool: {functionName}")
            };

            var toolCallId = toolCall.GetProperty("id").GetString();

            var serializedResult = JsonSerializer.Serialize(toolResult);
            Console.WriteLine($"DEBUG: Tool-resultat som skickas till AI: {serializedResult}");

            messages.Add(new
            {
                role = "tool",
                tool_call_id = toolCallId,
                content = serializedResult
            });
        }

        // 🔁 SECOND CALL (AI sammanställer SL-datan till ett trevligt svar)
        var finalResponse = await CallOpenAi(messages, tools);
        var choice = finalResponse.GetProperty("choices")[0].GetProperty("message");
        if (choice.TryGetProperty("content", out var content))
        {
            return content.GetString() ?? "Svaret var tomt.";
        }
        else
        {
            Console.WriteLine("DEBUG: OpenAI svarade utan content: " + finalResponse.ToString());
            return "Kunde inte tolka SL-datan.";
        }

    }

    private async Task<JsonElement> CallOpenAi(List<object> messages, object[]? tools)
    {
        var requestBody = new
        {
            model = "gpt-5.4",
            messages,
            tools,
            tool_choice = tools is null ? null : "auto"
        };

        var req = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/chat/completions"
        );

        req.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);

        req.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var res = await _http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
        {
            throw new Exception($"OpenAI failed: {(int)res.StatusCode} - {json}");
        }

        return JsonDocument.Parse(json).RootElement;
    }

    private async Task<object> HandleGetRouteAsync(JsonElement args)
    {
        var originQuery = args.GetProperty("originQuery").GetString() ?? string.Empty;
        var destQuery = args.GetProperty("destinationQuery").GetString() ?? string.Empty;

        var origin = await _tripService.FindBestStopAsync(originQuery);
        var dest = await _tripService.FindBestStopAsync(destQuery);

        if (origin is null || dest is null)
        {
            return new { error = "Kunde inte hitta en eller båda stationerna." };
        }

        var trips = await _tripService.GetTripsAsync(
            origin.Id,
            dest.Id,
            args.TryGetProperty("date", out var d) ? d.GetString() : null,
            args.TryGetProperty("time", out var t) ? t.GetString() : null,
            default
        );

        return trips;
    }

}