using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Features.GetUserAccess.Tasks.GetUserAccess;
using eFinanzer.Tests.Infrastructure;

namespace eFinanzer.Tests.Features.GetUserAccess.Tasks;

public class GetUserAccessTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private GetUserAccessTask MyTask => new(Database.CreateContext());

    private static ClaimsPrincipal CreateUser(params string[] groupPaths) => new(new ClaimsIdentity(
        groupPaths.Select(group => new Claim(UserClaims.GroupClaimType, group)), "Test"));

    private static FiscalYear CreateFiscalYear(string accountingUnitKey, string key, int year) => new()
    {
        Key = key,
        AccountingUnitKey = accountingUnitKey,
        StartDate = new DateOnly(year, 1, 1),
        EndDate = new DateOnly(year, 12, 31),
    };

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Context.AddRange(
            new AccountingUnit { Key = "fsr", Name = "FSR" },
            new AccountingUnit { Key = "stura", Name = "StuRa" },
            new AccountingUnit { Key = "zuse" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Get_NoGroups_ReturnsNoAccess()
    {
        // Act
        var result = await MyTask.Get(CreateUser(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.AccountingUnits);
        Assert.Empty(result.Value.AdministrationRoles);
        Assert.False(result.Value.ManageAccountingUnits);
        Assert.False(result.Value.ResolveRoleChangeRequests);
        Assert.False(result.Value.ManageAdministrationRoles);
    }

    [Fact]
    public async Task Get_AccountingUnitRoles_ReturnsOnlyTheseUnitsWithTheirRoles()
    {
        // Arrange
        var user = CreateUser(
            KeycloakGroups.GetAccountingUnitRoleGroup("stura", AccountingUnitRole.Treasurer),
            KeycloakGroups.GetAccountingUnitRoleGroup("fsr", AccountingUnitRole.Treasurer),
            KeycloakGroups.GetAccountingUnitRoleGroup("fsr", AccountingUnitRole.FinanceOfficer));

        // Act
        var result = await MyTask.Get(user, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.ManageAccountingUnits);
        Assert.False(result.Value.ManageAdministrationRoles);
        Assert.Empty(result.Value.AdministrationRoles);
        Assert.Collection(result.Value.AccountingUnits,
            fsr =>
            {
                Assert.Equal("fsr", fsr.Key);
                Assert.Equal("FSR", fsr.Name);
                Assert.Equal([AccountingUnitRole.FinanceOfficer, AccountingUnitRole.Treasurer], fsr.AccountingUnitRoles);
                Assert.True(fsr.ManageFiscalYears);
            },
            stura =>
            {
                Assert.Equal("stura", stura.Key);
                Assert.Equal([AccountingUnitRole.Treasurer], stura.AccountingUnitRoles);
                Assert.False(stura.ManageFiscalYears);
            });
    }

    [Fact]
    public async Task Get_UnitOnlyInToken_IsIgnored()
    {
        // Arrange
        var user = CreateUser(KeycloakGroups.GetAccountingUnitRoleGroup("deleted", AccountingUnitRole.FinanceOfficer));

        // Act
        var result = await MyTask.Get(user, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.AccountingUnits);
    }

    [Theory]
    [InlineData("/accounting-units/fsr")]
    [InlineData("/accounting-units/fsr/unknown-role")]
    [InlineData("/accounting-units/fsr/finance-officer/subgroup")]
    [InlineData("/accounting-units//finance-officer")]
    [InlineData("/other/fsr/finance-officer")]
    public async Task Get_GroupIsNoAccountingUnitRole_IsIgnored(string groupPath)
    {
        // Act
        var result = await MyTask.Get(CreateUser(groupPath), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.AccountingUnits);
    }

    [Theory]
    [InlineData(AdministrationRole.HeadFinanceOfficer)]
    [InlineData(AdministrationRole.FinanceAdministrator)]
    public async Task Get_ManagingAdministrator_ReturnsAllUnits(AdministrationRole role)
    {
        // Arrange
        var user = CreateUser(
            KeycloakGroups.GetAdministrationRoleGroup(role),
            KeycloakGroups.GetAccountingUnitRoleGroup("stura", AccountingUnitRole.Treasurer));

        // Act
        var result = await MyTask.Get(user, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.ManageAccountingUnits);
        Assert.Equal([role], result.Value.AdministrationRoles);
        Assert.Equal(["fsr", "stura", "zuse"], result.Value.AccountingUnits.Select(x => x.Key));
        Assert.Empty(result.Value.AccountingUnits.Single(x => x.Key == "fsr").AccountingUnitRoles);
        Assert.Equal([AccountingUnitRole.Treasurer], result.Value.AccountingUnits.Single(x => x.Key == "stura").AccountingUnitRoles);
    }

    [Fact]
    public async Task Get_FinanceAdministrator_ManagesFiscalYearsOfAllUnits()
    {
        // Arrange
        var user = CreateUser(KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator));

        // Act
        var result = await MyTask.Get(user, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.All(result.Value.AccountingUnits, x => Assert.True(x.ManageFiscalYears));
    }

    [Fact]
    public async Task Get_NonManagingAdministrator_ReturnsRolesWithoutUnits()
    {
        // Arrange
        var user = CreateUser(
            KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceReviewer),
            $"{KeycloakGroups.AdministrationGroup}/unknown-role");

        // Act
        var result = await MyTask.Get(user, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.ManageAccountingUnits);
        Assert.Equal([AdministrationRole.FinanceReviewer], result.Value.AdministrationRoles);
        Assert.Empty(result.Value.AccountingUnits);
    }

    [Theory]
    [InlineData(AdministrationRole.FinanceAdministrator, true, true)]
    [InlineData(AdministrationRole.HeadFinanceOfficer, true, false)]
    [InlineData(AdministrationRole.FinanceReviewer, false, false)]
    public async Task Get_AdministrationRole_ReturnsAdministrationPermissions(
        AdministrationRole role, bool resolveRoleChangeRequests, bool manageAdministrationRoles)
    {
        // Arrange
        var user = CreateUser(KeycloakGroups.GetAdministrationRoleGroup(role));

        // Act
        var result = await MyTask.Get(user, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(resolveRoleChangeRequests, result.Value.ResolveRoleChangeRequests);
        Assert.Equal(manageAdministrationRoles, result.Value.ManageAdministrationRoles);
    }

    [Fact]
    public async Task Get_UnitWithFiscalYears_ReturnsOnlyItsFiscalYearsNewestFirst()
    {
        // Arrange
        Context.AddRange(
            CreateFiscalYear("fsr", "a", 2025),
            CreateFiscalYear("fsr", "c", 2027),
            CreateFiscalYear("fsr", "b", 2026) with { Name = "HJ 2026" },
            CreateFiscalYear("stura", "x", 2026));
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var user = CreateUser(KeycloakGroups.GetAccountingUnitRoleGroup("fsr", AccountingUnitRole.Treasurer));

        // Act
        var result = await MyTask.Get(user, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var fsr = Assert.Single(result.Value.AccountingUnits);
        Assert.Equal(["c", "b", "a"], fsr.FiscalYears.Select(x => x.Key));
        Assert.Equal("HJ 2026", fsr.FiscalYears[1].Name);
    }
}
