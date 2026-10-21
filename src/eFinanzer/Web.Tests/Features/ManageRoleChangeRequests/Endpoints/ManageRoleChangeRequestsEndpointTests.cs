using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Pagination;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetAccountingUnits.Models;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetRoleChangeRequests.Models;
using eFinanzer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static eFinanzer.Tests.Features.ManageRoleChangeRequests.RoleChangeRequestTestData;

namespace eFinanzer.Tests.Features.ManageRoleChangeRequests.Endpoints;

/// <summary>
/// The tasks are covered by their own tests, these only cover what the endpoints add: authorization, the user taken
/// from the token and the mapping to HTTP
/// </summary>
public class ManageRoleChangeRequestsEndpointTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string BaseUrl = "/api/manage-role-change-requests";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly EFinanzerApiFactory _factory = new(database);

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await base.DisposeAsync();
    }

    private async Task<int> AddPendingRequest(string requestedBy)
    {
        var request = Pending(requestedBy, FixedTimeProvider.DefaultNow);
        Context.RoleChangeRequests.Add(request);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return request.Id;
    }

    private static HttpRequestMessage CreateRequest(string method, string url) => new(new HttpMethod(method), url)
    {
        // Valid body for the create and resolve endpoints, so the request reaches the authorization check
        Content = method switch
        {
            "POST" => JsonContent.Create(new { roleChanges = new[] { new { accountingUnitKey = "fsr", accountingUnitRole = "Treasurer", isAdded = true } } }),
            "PUT" => JsonContent.Create(new { resolution = "Dismissed" }),
            _ => null,
        },
    };

    public static TheoryData<string, string> AllEndpoints => new()
    {
        { "POST", $"{BaseUrl}/create-role-change-request" },
        { "GET", $"{BaseUrl}/list-own-role-change-requests" },
        { "GET", $"{BaseUrl}/list-pending-role-change-requests?pageSize=10" },
        { "PUT", $"{BaseUrl}/resolve-role-change-request?id=1" },
        { "DELETE", $"{BaseUrl}/delete-role-change-request?id=1" },
        { "GET", $"{BaseUrl}/list-accounting-units" },
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

    public static TheoryData<string, string> ResolverEndpoints => new()
    {
        { "GET", $"{BaseUrl}/list-pending-role-change-requests?pageSize=10" },
        { "PUT", $"{BaseUrl}/resolve-role-change-request?id=1" },
    };

    private static readonly string[] UsersWithoutResolvingRole =
    [
        KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceReviewer),
        KeycloakGroups.GetAccountingUnitRoleGroup("fsr", AccountingUnitRole.FinanceOfficer),
    ];

    public static TheoryData<string, string, string> ResolverEndpointsForUsersWithoutResolvingRole => new(
        from endpoint in ResolverEndpoints
        from groupPath in UsersWithoutResolvingRole
        select (endpoint.Data.Item1, endpoint.Data.Item2, groupPath));

    [Theory]
    [MemberData(nameof(ResolverEndpointsForUsersWithoutResolvingRole))]
    public async Task ResolverEndpoint_UserWithoutResolvingRole_ReturnsForbidden(string method, string url, string groupPath)
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(groupPath);

        // Act
        var response = await client.SendAsync(CreateRequest(method, url), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(AdministrationRole.FinanceAdministrator)]
    [InlineData(AdministrationRole.HeadFinanceOfficer)]
    public async Task GetPending_UserWithResolvingRole_ReturnsAllPendingRequests(AdministrationRole role)
    {
        // Arrange
        await AddPendingRequest(TestAuthHandler.Username);
        await AddPendingRequest("bob");
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(role));

        // Act
        var page = await client.GetFromJsonAsync<Page<GetRoleChangeRequestDto>>(
            $"{BaseUrl}/list-pending-role-change-requests?pageSize=10", JsonOptions, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, page!.Items.Count);
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task Resolve_UserWithResolvingRole_StoresUserFromToken()
    {
        // Arrange
        var ownRequestId = await AddPendingRequest(TestAuthHandler.Username);
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var response = await client.SendAsync(CreateRequest("PUT", $"{BaseUrl}/resolve-role-change-request?id={ownRequestId}"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await Context.RoleChangeRequests.SingleAsync(x => x.Id == ownRequestId, TestContext.Current.CancellationToken);
        Assert.Equal(RoleChangeRequest.ResolutionStatus.Dismissed, stored.Resolution);
        Assert.Equal(TestAuthHandler.Username, stored.ResolvedBy);
    }

    [Fact]
    public async Task Create_UserWithoutRoles_StoresRequestForUserFromToken()
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient();

        // Act
        var response = await client.SendAsync(CreateRequest("POST", $"{BaseUrl}/create-role-change-request"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(await Context.RoleChangeRequests.AnyAsync(x => x.RequestedBy == TestAuthHandler.Username,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Create_UnknownAccountingUnit_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsJsonAsync($"{BaseUrl}/create-role-change-request",
            new { roleChanges = new[] { new { accountingUnitKey = "unknown", accountingUnitRole = "Treasurer", isAdded = true } } },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOwn_UserWithoutRoles_ReturnsOnlyRequestsOfUserFromToken()
    {
        // Arrange
        var ownRequestId = await AddPendingRequest(TestAuthHandler.Username);
        await AddPendingRequest("bob");
        var client = _factory.CreateAuthorizedClient();

        // Act
        var requests = await client.GetFromJsonAsync<List<GetRoleChangeRequestDto>>(
            $"{BaseUrl}/list-own-role-change-requests", JsonOptions, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([ownRequestId], requests!.Select(x => x.Id));
    }

    [Fact]
    public async Task Delete_OwnRequest_ReturnsNoContent()
    {
        // Arrange
        var ownRequestId = await AddPendingRequest(TestAuthHandler.Username);
        var client = _factory.CreateAuthorizedClient();

        // Act
        var response = await client.DeleteAsync($"{BaseUrl}/delete-role-change-request?id={ownRequestId}",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await Context.RoleChangeRequests.AnyAsync(x => x.Id == ownRequestId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Delete_RequestOfOtherUser_ReturnsNotFound()
    {
        // Arrange
        var otherId = await AddPendingRequest("bob");
        var client = _factory.CreateAuthorizedClient();

        // Act
        var response = await client.DeleteAsync($"{BaseUrl}/delete-role-change-request?id={otherId}",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAccountingUnits_UserWithoutRoles_ReturnsAllAccountingUnits()
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient();

        // Act
        var accountingUnits = await client.GetFromJsonAsync<List<RoleChangeRequestAccountingUnitDto>>(
            $"{BaseUrl}/list-accounting-units", JsonOptions, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["fsr"], accountingUnits!.Select(x => x.Key));
    }
}
