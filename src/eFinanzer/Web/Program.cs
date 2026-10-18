using System.Diagnostics;
using System.Text.Json.Serialization;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Database;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Integrations.Keycloak;
using eFinanzer.Features.GetUsers;
using eFinanzer.Features.ManageAccountingUnits;
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

    var otlpEndpoint = builder.Configuration.GetValue<string>("Logging:OpenTelemetryExport:Endpoint");
    if (!string.IsNullOrEmpty(otlpEndpoint))
    {
        options.AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri(otlpEndpoint);
            otlp.Protocol = OtlpExportProtocol.HttpProtobuf;
        });
    }
    
});

builder.Services.AddProblemDetails();
builder.Services.AddMemoryCache();
builder.Services.AddExceptionHandler<BadHttpRequestExceptionHandler>();
builder.Services.AddAuth(builder.Configuration, builder.Environment);
builder.Services.AddKeycloakAdmin(builder.Configuration);
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddManageAccountingUnits();
builder.Services.AddGetUsers();
builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.ConfigureHttpJsonOptions(options =>
    // Enums are only accepted by name, so a number cannot smuggle in an undefined value
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<KeycloakSecuritySchemeTransformer>());

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

// Inside the request logging above, so the logged status code is the one produced by the exception handlers
app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi().AllowAnonymous();
app.MapScalarApiReference((options, httpContext) =>
{
    var request = httpContext.Request;

    options.WithTitle("eFinanzer")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .AddPreferredSecuritySchemes(KeycloakSecuritySchemeTransformer.SchemeName)
        .AddAuthorizationCodeFlow(KeycloakSecuritySchemeTransformer.SchemeName, flow =>
        {
            flow.ClientId = builder.Configuration.GetValue<string>("Authentication:OpenApiClientId");
            // Scalar sends an empty redirect_uri otherwise, which Keycloak rejects
            var scheme = request.Scheme;

#if RELEASE
            scheme = app.Environment.IsProduction() ? "https" : scheme; 
#endif
            
            flow.RedirectUri = $"{scheme}://{request.Host}{request.PathBase}/scalar/";
            flow.Pkce = Pkce.Sha256;
            flow.SelectedScopes = ["openid", "profile"];
        })
        .EnablePersistentAuthentication();
}).AllowAnonymous();

app.MapEndpoints();

app.Run();