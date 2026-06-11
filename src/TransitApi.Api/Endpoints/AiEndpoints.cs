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


        app.MapPost("/api/ai/ask", async (
            AiRequest request,
            HttpClient http,
            SlService sl,
            IOptions<OpenAiOptions> options
        ) =>
        {
            if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
                throw new Exception("Missing OpenAI API key");

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

                Never guess times.
            """
        },
        new
        {
            role = "user",
            content = request.Message
        }
    };

            // 🔁 FIRST CALL
            var firstResponse = await CallOpenAi(http, messages, tools, options.Value.ApiKey);

            var message = firstResponse
                .GetProperty("choices")[0]
                .GetProperty("message");





            // 🧠 NO TOOLS → return directly
            if (!message.TryGetProperty("tool_calls", out var toolCalls) || toolCalls.ValueKind != JsonValueKind.Array || toolCalls.GetArrayLength() == 0)
            {
                return Results.Ok(new
                {
                    answer = message.GetProperty("content").GetString()
                });
            }

            var assistantMessage = new
            {
                role = "assistant",
                content = message.GetProperty("content").GetString(),
                tool_calls = toolCalls
            };

            messages.Add(assistantMessage);

            foreach(var toolCall in toolCalls.EnumerateArray())
{
                var functionName = toolCall.GetProperty("function").GetProperty("name").GetString();
                var argsJson = toolCall.GetProperty("function").GetProperty("arguments").GetString();

                if (string.IsNullOrWhiteSpace(argsJson))
                    continue;

                using var args = JsonDocument.Parse(argsJson);

                object toolResult = functionName switch
                {
                    "get_next_departure" => await HandleNext(sl, args),
                    "get_departures" => await HandleDepartures(sl, args),
                    "search_stops" => await HandleSearch(sl, args),
                    _ => throw new Exception("Unknown tool")
                };

                Console.WriteLine(JsonSerializer.Serialize($"toolResult: {toolResult}"));

                var toolCallId = toolCall.GetProperty("id").GetString();

                messages.Add(new
                {
                    role = "tool",
                    tool_call_id = toolCallId,
                    content = JsonSerializer.Serialize(toolResult)
                });
            }

            // 🔁 SECOND CALL (AI formats final answer)
            var finalResponse = await CallOpenAi(http, messages, tools, options.Value.ApiKey);

            var answer = finalResponse
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

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