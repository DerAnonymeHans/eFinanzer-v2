using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit.Models;
using eFinanzer.Tests.Infrastructure;
using NSubstitute;

namespace eFinanzer.Tests.Features.ManageAccountingUnits.Endpoints;

/// <summary>
/// The tasks are covered by their own tests, these only cover what the endpoints add: authorization and the mapping
/// to HTTP
/// </summary>
public class ManageAccountingUnitsEndpointTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string BaseUrl = "/api/manage-accounting-units";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly EFinanzerApiFactory _factory = new(database);

    public override async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await base.DisposeAsync();
    }

    public static TheoryData<string, string> AllEndpoints => new()
    {
        { "POST", $"{BaseUrl}/create-accounting-unit" },
        { "GET", $"{BaseUrl}/list-accounting-units" },
        { "GET", $"{BaseUrl}/get-accounting-unit?key=fsr" },
        { "PUT", $"{BaseUrl}/update-accounting-unit?key=fsr" },
        { "DELETE", $"{BaseUrl}/delete-accounting-unit?key=fsr" },
    };

    private static HttpRequestMessage CreateRequest(string method, string url) => new(new HttpMethod(method), url)
    {
        // Valid body for the create and update endpoints, so the request reaches the authorization check
        Content = method switch
        {
            "POST" => JsonContent.Create(new { key = "fsr" }),
            "PUT" => JsonContent.Create(new { name = (string?)null, description = (string?)null, usersWithRoles = Array.Empty<object>() }),
            _ => null,
        },
    };

    [Theory]
    [MemberData(nameof(AllEndpoints))]
    public async Task Endpoint_AnonymousUser_ReturnsUnauthorized(string method, string url)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.SendAsync(CreateRequest(method, url), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AllEndpoints))]
    public async Task Endpoint_UserWithoutManagingRole_ReturnsForbidden(string method, string url)
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(
            KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceReviewer),
            KeycloakGroups.GetAccountingUnitRoleGroup("fsr", AccountingUnitRole.FinanceOfficer));

        // Act
        var response = await client.SendAsync(CreateRequest(method, url), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(AdministrationRole.HeadFinanceOfficer)]
    [InlineData(AdministrationRole.FinanceAdministrator)]
    public async Task Create_UserWithManagingRole_ReturnsCreated(AdministrationRole role)
    {
        // Arrange
        _factory.KeycloakUserClient.SetupGroupCreation();
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(role));

        // Act
        var response = await client.PostAsJsonAsync($"{BaseUrl}/create-accounting-unit", new { key = "fsr" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Create_InvalidKey_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var response = await client.PostAsJsonAsync($"{BaseUrl}/create-accounting-unit", new { key = "fsr inf" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_KeyAlreadyExists_ReturnsConflict()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var response = await client.PostAsJsonAsync($"{BaseUrl}/create-accounting-unit", new { key = "fsr" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Get_ExistingUnit_ReturnsUnitWithRolesAsStrings()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr", Name = "FSR" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _factory.KeycloakUserClient.SetupRoleMembers("fsr", (AccountingUnitRole.Treasurer, ["alice"]));
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.HeadFinanceOfficer));

        // Act
        var response = await client.GetAsync($"{BaseUrl}/get-accounting-unit?key=fsr", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"Treasurer\"", json);
        var dto = JsonSerializer.Deserialize<GetAccountingUnitManagementDto>(json, JsonOptions)!;
        Assert.Equal("FSR", dto.Name);
        var user = Assert.Single(dto.UsersWithRoles);
        Assert.Equal("alice", user.UserName);
        Assert.Equal([AccountingUnitRole.Treasurer], user.Roles);
    }

    [Fact]
    public async Task Delete_UnknownKey_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var response = await client.DeleteAsync($"{BaseUrl}/delete-accounting-unit?key=unknown", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("""{ "name": null, "description": null }""")]
    [InlineData("""{ "name": null, "description": null, "usersWithRoles": [{ "userName": "alice", "roles": [1] }] }""")]
    public async Task Update_InvalidBody_ReturnsBadRequest(string body)
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var response = await client.PutAsync($"{BaseUrl}/update-accounting-unit?key=fsr",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_factory.KeycloakUserClient.ReceivedCalls());
    }
}
