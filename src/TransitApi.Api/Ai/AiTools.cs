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
                name = "get_route",
                description = "Plan a complete trip, check travel times, or find out when to travel between two stops or stations (origin and destination). Use this for questions like 'How do I get from A to B?' or 'When does the bus/trip go from Orminge centrum to Slussen?'.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        originQuery = new { type = "string", description = "The name of the origin station, e.g. Årstadal" },
                        destinationQuery = new { type = "string", description = "The name of the destination station, e.g. Liljeholmen" },
                        date = new { type = "string", description = "Optional: YYYYMMDD" },
                        time = new { type = "string", description = "Optional: HHmm" }
                    },
                    required = new[] { "originQuery", "destinationQuery" }
                }
            }
        },

        new
        {
            type = "function",
            function = new
            {
                name = "get_next_departure",
                description = "Get the single next departure for a specific line at a stop.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "The name of the stop or station" },
                        line = new { type = "integer", description = "The line number (e.g., 19, 444)" },
                        destination = new { type = "string", description = "The name of the final stop" },
                        transport = new { type = "string", description = "Optional: The transport type (e.g., BUS, METRO, TRAIN, TRAM, FERRY)" }
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
                description = "Get upcoming departures for a specific station/site.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "The name of the stop or station" },
                        destination = new { type = "string", description = "Optional: The destination to filter by" },
                        transport = new { type = "string", description = "Optional filter: The transport type" }
                    },
                    required = new[] { "query" }
                }
            }
        }
    ];
}