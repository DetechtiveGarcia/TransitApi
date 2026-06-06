using Scalar.AspNetCore;

namespace TransitApi.Api.OpenApi;

public static class OpenApiEndpointsExtensions
{
    public static WebApplication MapOpenApiEndpoints(this WebApplication app)
    {
        app.MapOpenApi();

        app.MapScalarApiReference("/docs", options =>
        {
            options.WithTitle("Transit API Documentation");
        });

        return app;
    }
}