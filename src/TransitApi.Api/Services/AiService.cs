using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
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
                content = """
                    You are a transit AI for Stockholm public transport.
                    You MUST use tools for:
                    - next departures
                    - stops
                    - bus lines
                    Never guess times. Respond in Swedish.
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

        return finalResponse
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "Kunde inte tolka SL-datan.";
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