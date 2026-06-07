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
                description = "Get next bus departure by stop and line",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string" },
                        line = new { type = "integer" }
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
                description = "Get multiple upcoming departures from a stop",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string" }
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
                description = "Find nearest matching stop by name",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string" }
                    },
                    required = new[] { "query" }
                }
            }
        }
    ];
}