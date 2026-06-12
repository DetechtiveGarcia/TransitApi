namespace TransitApi.Api.Ai;

public static class AiTools
{
    public static object[] GetTools() =>
    [
        new
        {
            type = "function",
            function = new
            {
                name = "get_next_departure",
                description = "Get the single next departure for a specific line at a stop. Use this for specific questions like 'When is the next bus 19?'",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "The name of the stop or station" },
                        line = new { type = "integer", description = "The line number (e.g., 19, 444)" }
                    },
                    required = new[] { "query", "line" }
                }
            }
        },

        new
        {
            type = "function",
            function = new
            {
                name = "get_departures",
                description = "Get upcoming departures from a stop, including metro, bus, train, tram, and ferry. Supports optional destination filtering.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "The name of the stop or station" },
                        destination = new { type = "string", description = "Optional: The destination of the trip to filter by" }
                    },
                    required = new[] { "query" }
                }
            }
        },

        new
        {
            type = "function",
            function = new
            {
                name = "search_stops",
                description = "Search for a stop or station name in Stockholm to get its ID.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "The search term for the stop" }
                    },
                    required = new[] { "query" }
                }
            }
        }
    ];
}