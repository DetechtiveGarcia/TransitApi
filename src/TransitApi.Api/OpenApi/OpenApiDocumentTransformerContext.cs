using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace TransitApi.Api.OpenApi;

public sealed class OpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();

        document.Info.Title = "Transit API";
        document.Info.Description = """
        ## 🚌 Transit API

        Transit is a voice-first transit API that provides real-time public transport information.

        It is designed to power applications where users can ask natural language questions like:

        - "When is the next bus to Slussen from Orminge?"
        - "How do I get to Kista right now?"
        - "Is there a bus coming soon?"

        ### Features

        - Real-time departures (SL integration)
        - Line-based filtering
        - Stop-based queries
        - Built for voice + AI assistants
        - Fast minimal API design

        ### Example use cases

        - Mobile apps (React Native / iOS / Android)
        - Voice assistants
        - AI travel planners
        - Smart kiosks / transport screens
        """;

        document.Info.Version = "v1";

        return Task.CompletedTask;
    }
}