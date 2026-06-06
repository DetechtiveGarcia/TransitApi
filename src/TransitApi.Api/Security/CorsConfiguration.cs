namespace TransitApi.Api.Security;

public static class CorsConfiguration
{
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("All", policy =>
            {
                policy
                    .WithOrigins("*")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });


        return services;
    }
}