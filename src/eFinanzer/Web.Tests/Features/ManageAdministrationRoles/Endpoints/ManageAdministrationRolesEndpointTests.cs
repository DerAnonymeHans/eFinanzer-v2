using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.GetAdministrationUsers.Models;
using eFinanzer.Tests.Infrastructure;
using NSubstitute;

namespace eFinanzer.Tests.Features.ManageAdministrationRoles.Endpoints;

/// <summary>
/// The tasks are covered by their own tests, these only cover what the endpoints add: authorization and the mapping
/// to HTTP
/// </summary>
public class ManageAdministrationRolesEndpointTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string BaseUrl = "/api/manage-administration-roles";

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
        { "GET", $"{BaseUrl}/get-administration-users" },
        { "PUT", $"{BaseUrl}/update-administration-roles" },
    };

    private static HttpRequestMessage CreateRequest(string method, string url) => new(new HttpMethod(method), url)
    {
        // Valid body for the update endpoint, so the request reaches the authorization check
        Content = method == "PUT"
            ? JsonContent.Create(new { username = "alice", administrationRoles = Array.Empty<string>() })
            : null,
    };

    private HttpClient CreateManagingClient()
        => _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

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
    public async Task Endpoint_UserWithoutAdministrationRole_ReturnsForbiddenWithoutCallingKeycloak(string method, string url)
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(
            KeycloakGroups.GetAccountingUnitRoleGroup("fsr", AccountingUnitRole.FinanceOfficer));

        // Act
        var response = await client.SendAsync(CreateRequest(method, url), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(_factory.KeycloakUserClient.ReceivedCalls());
    }

    /// <summary>
    /// Only finance administrators change administration roles, even head finance officers may not
    /// </summary>
    [Theory]
    [InlineData(AdministrationRole.HeadFinanceOfficer)]
    [InlineData(AdministrationRole.FinanceReviewer)]
    public async Task Update_UserWithoutManagingRole_ReturnsForbiddenWithoutCallingKeycloak(AdministrationRole role)
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(role));

        // Act
        var response = await client.SendAsync(CreateRequest("PUT", $"{BaseUrl}/update-administration-roles"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(_factory.KeycloakUserClient.ReceivedCalls());
    }

    /// <summary>
    /// Every member of the administration may see who else is in it
    /// </summary>
    [Theory]
    [InlineData(AdministrationRole.HeadFinanceOfficer)]
    [InlineData(AdministrationRole.FinanceAdministrator)]
    [InlineData(AdministrationRole.FinanceReviewer)]
    public async Task Get_UserWithAdministrationRole_ReturnsUsersWithRolesAsStrings(AdministrationRole role)
    {
        // Arrange
        _factory.KeycloakUserClient.SetupAdministrationRoleMembers((AdministrationRole.FinanceReviewer, ["alice"]));
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(role));

        // Act
        var response = await client.GetAsync($"{BaseUrl}/get-administration-users", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"FinanceReviewer\"", json);
        var dto = JsonSerializer.Deserialize<GetAdministrationUsersDto>(json, JsonOptions)!;
        var user = Assert.Single(dto.AdministrationUsers);
        Assert.Equal("alice", user.Username);
        Assert.Equal([AdministrationRole.FinanceReviewer], user.Roles);
    }

    [Fact]
    public async Task Update_UserWithManagingRole_ReturnsNoContent()
    {
        // Arrange
        _factory.KeycloakUserClient.SetupAdministrationRoleMembers();
        _factory.KeycloakUserClient.SetupUsers("alice");
        var client = CreateManagingClient();

        // Act
        var response = await client.PutAsJsonAsync($"{BaseUrl}/update-administration-roles",
            new { username = "alice", administrationRoles = new[] { "FinanceReviewer" } }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await _factory.KeycloakUserClient.Received(1).AddUserToGroup("id-alice",
            KeycloakUserClientExtensions.AdministrationRoleGroup(AdministrationRole.FinanceReviewer).Id, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Otherwise the last finance administrator could lock everyone out of managing administration roles
    /// </summary>
    [Fact]
    public async Task Update_RemoveOwnManagingRole_ReturnsBadRequestWithoutCallingKeycloak()
    {
        // Arrange
        var client = CreateManagingClient();

        // Act
        var response = await client.PutAsJsonAsync($"{BaseUrl}/update-administration-roles",
            new { username = TestAuthHandler.Username, administrationRoles = new[] { "FinanceReviewer" } },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_factory.KeycloakUserClient.ReceivedCalls());
    }

    [Fact]
    public async Task Update_UnknownUser_ReturnsBadRequest()
    {
        // Arrange
        _factory.KeycloakUserClient.SetupAdministrationRoleMembers();
        _factory.KeycloakUserClient.GetUserByUsername("mallory", Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUser>.Fail(new NotFoundError("User", "mallory")));
        var client = CreateManagingClient();

        // Act
        var response = await client.PutAsJsonAsync($"{BaseUrl}/update-administration-roles",
            new { username = "mallory", administrationRoles = new[] { "HeadFinanceOfficer" } }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
