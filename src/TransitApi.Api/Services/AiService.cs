using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TransitApi.Api.Ai;
using TransitApi.Api.Endpoints;
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
                        4. If you cannot find a direct route or matching departure for a specific request, 
                           you MUST inspect the JSON object returned by the tool. If the JSON contains an 
                           'alternatives' field, you are REQUIRED to present those options to the user. 
                           Do not just say no; use the 'alternatives' data to be helpful.
                        5. LANGUAGE: Always respond in the same language that the user is currently using.
                        6. Use correct Swedish for traffic information.
                           When writing about buses, use the word 'avgår' instead of 'avfärdar' or other direct English translations.
                        7. If the next departure is leaving immediately, or if the user asks for a specific 
                           line that isn't available, provide at least the next 2-3 alternative departures 
                           so the user has options.
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
                "search_stops" => await AiEndpoints.HandleSearch(_sl, args),
                _ => throw new Exception("Unknown tool")
            };

            var toolCallId = toolCall.GetProperty("id").GetString();

            var serializedResult = JsonSerializer.Serialize(toolResult);
            Console.WriteLine($"DEBUG: Tool-resultat som skickas till AI: {serializedResult}");

            messages.Add(new
            {
                role = "tool",
                tool_call_id = toolCallId,
                content = JsonSerializer.Serialize(toolResult)
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

}