using System.Diagnostics;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Helper;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using Scalar.AspNetCore;

namespace eFinanzer;

public static class RegistrationExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) => app.Use(async (context, next) =>
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
    
    public static IApplicationBuilder UseUsernameLogging(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var username = context.User.Identity?.Name;
        if (username is null)
        {
            await next();
            return;
        }

        using var logContext = logger.AddContext("Username", username);
        await next();
    });
    
    public static IEndpointConventionBuilder MapScalar(this IEndpointRouteBuilder app, WebApplicationBuilder builder) 
        => app.MapScalarApiReference((options, httpContext) =>
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
                    scheme = builder.Environment.IsProduction() ? "https" : scheme; 
                    #endif
                
                    flow.RedirectUri = $"{scheme}://{request.Host}{request.PathBase}/scalar/";
                    flow.Pkce = Pkce.Sha256;
                    flow.SelectedScopes = ["openid", "profile"];
                })
                .EnablePersistentAuthentication();
        });
    
    public static ILoggingBuilder AddOpenTelemetry(this WebApplicationBuilder builder) => builder.Logging.AddOpenTelemetry(options =>
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
}