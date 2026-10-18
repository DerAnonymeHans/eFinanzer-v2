using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Features.GetUserAccess.Tasks.GetUserAccess.Models;
using eFinanzer.Tests.Infrastructure;

namespace eFinanzer.Tests.Features.GetUserAccess.Endpoints;

/// <summary>
/// The task is covered by its own tests, these only cover what the endpoint adds: authorization and the mapping
/// to HTTP
/// </summary>
public class GetUserAccessEndpointTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string Url = "/api/get-user-access/my-user-access";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly EFinanzerApiFactory _factory = new(database);

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Context.AddRange(
            new AccountingUnit { Key = "fsr", Name = "FSR" },
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

    [Fact]
    public async Task Get_AnonymousUser_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(Url, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_UserWithoutRoles_ReturnsNoAccess()
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient();

        // Act
        var access = await client.GetFromJsonAsync<UserAccess>(Url, JsonOptions, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(access);
        Assert.Empty(access.AccountingGroups);
        Assert.Empty(access.AdministrationRoles);
        Assert.False(access.ManageAccountingUnits);
    }

    [Fact]
    public async Task Get_UserWithRoles_ReturnsAccess()
    {
        // Arrange
        var client = _factory.CreateAuthorizedClient(
            KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceReviewer),
            KeycloakGroups.GetAccountingUnitRoleGroup("fsr", AccountingUnitRole.FinanceOfficer));

        // Act
        var access = await client.GetFromJsonAsync<UserAccess>(Url, JsonOptions, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(access);
        Assert.False(access.ManageAccountingUnits);
        Assert.Equal([AdministrationRole.FinanceReviewer], access.AdministrationRoles);
        var fsr = Assert.Single(access.AccountingGroups);
        Assert.Equal("fsr", fsr.Key);
        Assert.Equal("FSR", fsr.Name);
        Assert.True(fsr.ManageFiscalYears);
        Assert.Equal([AccountingUnitRole.FinanceOfficer], fsr.AccountingUnitRoles);
        Assert.Equal(new UserAccess.FiscalYearDto { Key = "2026" }, Assert.Single(fsr.FiscalYears));
    }
}
