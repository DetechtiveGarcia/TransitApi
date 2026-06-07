using Microsoft.AspNetCore.OpenApi;
using TransitApi.Api.Endpoints;
using TransitApi.Api.OpenAi;
using TransitApi.Api.OpenApi;
using TransitApi.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OpenAiOptions>(
    builder.Configuration.GetSection("OpenAI"));
builder.Services.AddOpenApi();
builder.Services.AddHttpClient<SlService>();
builder.Services.AddHttpClient();

var app = builder.Build();


app.MapOpenApi();


app.UseHttpsRedirection();
app.MapOpenApiEndpoints();
app.MapTransitEndpoints();
app.MapAiEndpoints();

app.Run();