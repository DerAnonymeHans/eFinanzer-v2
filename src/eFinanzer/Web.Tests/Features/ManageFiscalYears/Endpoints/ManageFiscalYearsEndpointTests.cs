using System.Net;
using System.Net.Http.Json;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Features.ManageFiscalYears.Tasks.GetFiscalYears.Models;
using eFinanzer.Tests.Infrastructure;

namespace eFinanzer.Tests.Features.ManageFiscalYears.Endpoints;

/// <summary>
/// The tasks are covered by their own tests, these only cover what the endpoints add: authorization and the mapping
/// to HTTP
/// </summary>
public class ManageFiscalYearsEndpointTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string BaseUrl = "/api/manage-fiscal-years";

    private readonly EFinanzerApiFactory _factory = new(database);

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Context.AddRange(
            new AccountingUnit { Key = "fsr" },
            new FiscalYear
            {
                Key = "2026",
                AccountingUnitKey = "fsr",
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31),
            });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Every endpoint with the status code it returns for an authorized user
    /// </summary>
    private static readonly (string Method, string Url, HttpStatusCode SuccessStatusCode)[] Endpoints =
    [
        ("POST", $"{BaseUrl}/create-fiscal-year", HttpStatusCode.Created),
        ("GET", $"{BaseUrl}/list-fiscal-years?accountingUnitKey=fsr", HttpStatusCode.OK),
        ("PUT", $"{BaseUrl}/update-fiscal-year?accountingUnitKey=fsr&key=2026", HttpStatusCode.NoContent),
        ("DELETE", $"{BaseUrl}/delete-fiscal-year?accountingUnitKey=fsr&key=2026", HttpStatusCode.NoContent),
    ];

    public static TheoryData<string, string> AllEndpoints => new(Endpoints.Select(x => (x.Method, x.Url)));

    private static HttpRequestMessage CreateRequest(string method, string url) => new(new HttpMethod(method), url)
    {
        // Valid body for the create and update endpoints, so the request reaches the authorization check
        Content = method switch
        {
            "POST" => JsonContent.Create(new { key = "2027", accountingUnitKey = "fsr", startDate = "2027-01-01", endDate = "2027-12-31" }),
            "PUT" => JsonContent.Create(new { name = "HJ 2026", startDate = "2026-01-01", endDate = "2026-12-31" }),
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

    private static readonly string[] UsersWithoutManagingRole =
    [
        KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceReviewer),
        KeycloakGroups.GetAccountingUnitRoleGroup("fsr", AccountingUnitRole.Treasurer),
        KeycloakGroups.GetAccountingUnitRoleGroup("fsr", AccountingUnitRole.FinanceAuthorizedUser),
        KeycloakGroups.GetAccountingUnitRoleGroup("stura", AccountingUnitRole.FinanceOfficer),
    ];

    public static TheoryData<string, string, string> AllEndpointsForUsersWithoutManagingRole => new(
        from endpoint in Endpoints
        from groupPath in UsersWithoutManagingRole
        select (endpoint.Method, endpoint.Url, groupPath));

    [Theory]
    [MemberData(nameof(AllEndpointsForUsersWithoutManagingRole))]
    public async Task Endpoint_UserWithoutManagingRole_ReturnsForbidden(string method, string url, string groupPath)
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(groupPath);

        // Act
        var response = await client.SendAsync(CreateRequest(method, url), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static readonly string[] UsersWithManagingRole =
    [
        KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.HeadFinanceOfficer),
        KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator),
        KeycloakGroups.GetAccountingUnitRoleGroup("fsr", AccountingUnitRole.FinanceOfficer),
    ];

    public static TheoryData<string, string, HttpStatusCode, string> AllEndpointsForUsersWithManagingRole => new(
        from endpoint in Endpoints
        from groupPath in UsersWithManagingRole
        select (endpoint.Method, endpoint.Url, endpoint.SuccessStatusCode, groupPath));

    [Theory]
    [MemberData(nameof(AllEndpointsForUsersWithManagingRole))]
    public async Task Endpoint_UserWithManagingRole_Succeeds(string method, string url, HttpStatusCode expected, string groupPath)
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(groupPath);

        // Act
        var response = await client.SendAsync(CreateRequest(method, url), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Create_FinanceOfficerForOtherUnitInBody_ReturnsForbidden()
    {
        // Arrange
        Context.AccountingUnits.Add(new AccountingUnit { Key = "stura" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAccountingUnitRoleGroup("fsr", AccountingUnitRole.FinanceOfficer));

        // Act
        var response = await client.PostAsJsonAsync($"{BaseUrl}/create-fiscal-year",
            new { key = "2027", accountingUnitKey = "stura", startDate = "2027-01-01", endDate = "2027-12-31" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(Context.FiscalYears.Any(x => x.AccountingUnitKey == "stura"));
    }

    [Fact]
    public async Task Create_InvalidDates_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var response = await client.PostAsJsonAsync($"{BaseUrl}/create-fiscal-year",
            new { key = "2027", accountingUnitKey = "fsr", startDate = "2027-12-31", endDate = "2027-01-01" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_KeyAlreadyExists_ReturnsConflict()
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var response = await client.PostAsJsonAsync($"{BaseUrl}/create-fiscal-year",
            new { key = "2026", accountingUnitKey = "fsr", startDate = "2026-01-01", endDate = "2026-12-31" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_OverlapsExistingFiscalYear_ReturnsConflict()
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var response = await client.PostAsJsonAsync($"{BaseUrl}/create-fiscal-year",
            new { key = "2027", accountingUnitKey = "fsr", startDate = "2026-07-01", endDate = "2027-06-30" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("overlaps the existing fiscal year '2026'", body);
    }

    [Fact]
    public async Task Update_OverlapsOtherFiscalYear_ReturnsConflict()
    {
        // Arrange
        Context.FiscalYears.Add(new FiscalYear
        {
            Key = "2027",
            AccountingUnitKey = "fsr",
            StartDate = new DateOnly(2027, 1, 1),
            EndDate = new DateOnly(2027, 12, 31),
        });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var response = await client.PutAsJsonAsync($"{BaseUrl}/update-fiscal-year?accountingUnitKey=fsr&key=2026",
            new { name = (string?)null, startDate = "2026-01-01", endDate = "2027-01-01" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task List_ExistingUnit_ReturnsFiscalYears()
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.HeadFinanceOfficer));

        // Act
        var fiscalYears = await client.GetFromJsonAsync<List<GetFiscalYearManagementListDto>>(
            $"{BaseUrl}/list-fiscal-years?accountingUnitKey=fsr", TestContext.Current.CancellationToken);

        // Assert
        var fiscalYear = Assert.Single(fiscalYears!);
        Assert.Equal(new GetFiscalYearManagementListDto
        {
            Key = "2026",
            AccountingUnitKey = "fsr",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ReceiptsCount = 0,
            BudgetsCount = 0,
        }, fiscalYear);
    }

    [Fact]
    public async Task List_UnknownUnit_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.HeadFinanceOfficer));

        // Act
        var response = await client.GetAsync($"{BaseUrl}/list-fiscal-years?accountingUnitKey=unknown",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_UnknownKey_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var response = await client.SendAsync(CreateRequest("PUT", $"{BaseUrl}/update-fiscal-year?accountingUnitKey=fsr&key=unknown"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownKey_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var response = await client.DeleteAsync($"{BaseUrl}/delete-fiscal-year?accountingUnitKey=fsr&key=unknown",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
