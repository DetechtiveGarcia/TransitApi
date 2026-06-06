using TransitApi.Api.Endpoints;
using TransitApi.Api.OpenApi;
using TransitApi.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient<SlService>();

var app = builder.Build();


app.MapOpenApi();


app.UseHttpsRedirection();
app.MapOpenApiEndpoints();
app.MapTransitEndpoints();

app.Run();