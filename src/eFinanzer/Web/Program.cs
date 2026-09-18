using System.Diagnostics;
using Microsoft.AspNetCore.HttpLogging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddOpenTelemetry(options =>
{
    var openTelemetryBuilder = ResourceBuilder.CreateDefault()
        .AddService(serviceName: "eFinanzer", serviceNamespace: builder.Environment.EnvironmentName);
    options.SetResourceBuilder(openTelemetryBuilder);
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;

    options.AddOtlpExporter(otlp =>
    {
        otlp.Endpoint = new Uri(builder.Configuration.GetValue<string>("Logging:OpenTelemetryExport:Endpoint")!);
        otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
    });
    
});

builder.Services.AddOpenApi();

var app = builder.Build();

app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();

    await next();

    stopwatch.Stop();

    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    // OpenTelemetry captures structured parameters as OTLP Log Attributes
    logger.LogInformation(
        "HTTP {HttpMethod} {HttpPath} responded {HttpStatusCode} in {ElapsedMs:0.0000} ms",
        context.Request.Method,
        context.Request.Path.Value,
        context.Response.StatusCode,
        stopwatch.Elapsed.TotalMilliseconds
    );
});

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("eFinanzer")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});
app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (ILogger<Program> logger) =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        logger.LogInformation("Created {count} forecast", forecast.Length);
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}