using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TransitApi.Api.Ai;
using TransitApi.Api.OpenAi;

namespace TransitApi.Api.Services;

public class AiService
{
    private readonly HttpClient _http;
    private readonly SlService _sl;
    private readonly string _apiKey;

    public AiService(HttpClient http, SlService sl, IConfiguration config)
    {
        _http = http;
        _sl = sl;
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
                        You are a professional transit AI for Stockholm public transport (SL).
                        You have access to tools for departures, stops, and all transport modes (metro, bus, train, tram, ferry).
                    
                        INSTRUCTIONS:
                        1. Always use tools for departures, stops, and lines. Never guess times.
                        2. If the user specifies a destination (e.g., 'to Slussen'), always provide 
                           that destination as an argument to the tool so the backend can filter results.
                        3. If no specific destination is mentioned, provide the general next departures.
                        4. If you cannot find a direct route or matching departure, communicate this clearly.
                        5. LANGUAGE: Always respond in the same language that the user is currently using.
                        6. Use correct Swedish for traffic information.
                           When writing about buses, use the word 'avgår' instead of 'avfärdar' or other direct English translations.
                        7. If the next bus is leaving immediately, provide at least the next 2-3 departures so the user has options. 
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
                "get_next_departure" => await HandleNext(_sl, args),
                "get_departures" => await HandleDepartures(_sl, args),
                "search_stops" => await HandleSearch(_sl, args),
                _ => throw new Exception("Unknown tool")
            };

            var toolCallId = toolCall.GetProperty("id").GetString();

            messages.Add(new
            {
                role = "tool",
                tool_call_id = toolCallId,
                content = JsonSerializer.Serialize(toolResult)
            });
        }

        // 🔁 SECOND CALL (AI sammanställer SL-datan till ett trevligt svar)
        var finalResponse = await CallOpenAi(messages, tools);

        var rawText = finalResponse
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "Kunde inte tolka SL-datan.";

        return CleanTextForSpeech(rawText);
    }

    private async Task<JsonElement> CallOpenAi(List<object> messages, object[]? tools)
    {
        var requestBody = new
        {
            model = "gpt-4o-mini",
            messages,
            tools,
            tool_choice = tools is null ? null : "auto"
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
        {
            throw new Exception($"OpenAI chat failed: {(int)res.StatusCode} - {json}");
        }

        return JsonDocument.Parse(json).RootElement;
    }

    // --- De existerande tool-handlers som du skrivit ---
    private static async Task<object> HandleNext(SlService sl, JsonDocument args)
    {
        var query = args.RootElement.GetProperty("query").GetString()!;
        var line = args.RootElement.GetProperty("line").GetInt32();
        var site = (await sl.SearchSites(query)).FirstOrDefault();
        if (site is null) return new { error = "site not found" };

        var departures = await sl.GetDepartures(site.Id);
        var next = departures.Where(d => d.LineId == line).OrderBy(d => d.Expected).FirstOrDefault();
        return next is null ? new { error = "no departures" } : next;
    }

    private static async Task<object> HandleDepartures(SlService sl, JsonDocument args)
    {
        var query = args.RootElement.GetProperty("query").GetString()!;

        // Hämta destination om den finns i anropet
        string? destination = args.RootElement.TryGetProperty("destination", out var dest)
                              ? dest.GetString()
                              : null;

        var site = (await sl.SearchSites(query)).FirstOrDefault();
        if (site is null) return new { error = "site not found" };

        var departures = await sl.GetDepartures(site.Id);

        // Om användaren har angett en destination, filtrera listan
        if (!string.IsNullOrWhiteSpace(destination))
        {
            var filtered = departures.Where(d =>
                d.Destination.Contains(destination, StringComparison.OrdinalIgnoreCase));

            return filtered.Any() ? filtered.ToList() : new { error = "no departures to that destination" };
        }

        return departures.Take(3).ToList();
    }

    private static async Task<object> HandleSearch(SlService sl, JsonDocument args)
    {
        var query = args.RootElement.GetProperty("query").GetString()!;
        return await sl.SearchSites(query);
    }

    private string CleanTextForSpeech(string text)
    {
        // Tar bort allt som ser ut som Markdown-länkar [text](url)
        string cleanText = Regex.Replace(text, @"\[([^\]]+)\]\([^)]+\)", "$1");

        // Tar bort andra tecken (stjärnor, hashtaggar etc.)
        cleanText = Regex.Replace(cleanText, @"[*#_`\[\]]", "");

        return cleanText.Trim();
    }
}