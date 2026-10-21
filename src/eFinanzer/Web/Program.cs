using System.Text.Json.Serialization;
using eFinanzer;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Database;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Integrations.Keycloak;
using eFinanzer.Features.GetUserAccess;
using eFinanzer.Features.GetUsers;
using eFinanzer.Features.ManageAccountingUnits;
using eFinanzer.Features.ManageFiscalYears;
using eFinanzer.Features.ManageRoleChangeRequests;

var builder = WebApplication.CreateBuilder(args);

builder.AddOpenTelemetry();

builder.Services.AddProblemDetails();
builder.Services.AddMemoryCache();
builder.Services.AddExceptionHandler<BadHttpRequestExceptionHandler>();
builder.Services.AddAuth(builder.Configuration, builder.Environment);
builder.Services.AddFrontendCors(builder.Configuration);
builder.Services.AddKeycloakAdmin(builder.Configuration);
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddManageAccountingUnits();
builder.Services.AddManageFiscalYears();
builder.Services.AddManageRoleChangeRequests();
builder.Services.AddGetUsers();
builder.Services.AddGetUserAccess();
builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.ConfigureHttpJsonOptions(options =>
    // Enums are only accepted by name, so a number cannot smuggle in an undefined value
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<KeycloakSecuritySchemeTransformer>());

var app = builder.Build();

app.UseRequestLogging();

// Inside the request logging above, so the logged status code is the one produced by the exception handlers
app.UseExceptionHandler();

if (builder.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

// Before authentication, so preflight requests (which never carry a token) are answered instead of rejected
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseUsernameLogging();

app.MapOpenApi().AllowAnonymous();
app.MapScalar(builder).AllowAnonymous();

app.MapEndpoints();

app.Run();