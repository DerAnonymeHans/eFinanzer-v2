using System.Security.Claims;
using eFinanzer.Common.Auth;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;

namespace eFinanzer.Tests.Common.Auth;

public class UserClaimsTests
{
    private static readonly string HeadFinanceOfficer = KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.HeadFinanceOfficer);
    private static readonly string FinanceAdministrator = KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceAdministrator);
    private static readonly string FinanceReviewer = KeycloakGroups.GetAdministrationRoleGroup(AdministrationRole.FinanceReviewer);
    private static readonly string FsrFinanceOfficer = KeycloakGroups.GetAccountingUnitRoleGroup("fsr", AccountingUnitRole.FinanceOfficer);
    private static readonly string FsrTreasurer = KeycloakGroups.GetAccountingUnitRoleGroup("fsr", AccountingUnitRole.Treasurer);
    private static readonly string SturaFinanceOfficer = KeycloakGroups.GetAccountingUnitRoleGroup("stura", AccountingUnitRole.FinanceOfficer);

    private static ClaimsPrincipal CreateUser(params string[] groupPaths)
        => new(new ClaimsIdentity(groupPaths.Select(x => new Claim(UserClaims.ClaimType, x)), "Test"));

    [Fact]
    public void IsInGroup_MemberOfSubgroup_ReturnsFalseForParent()
    {
        // Arrange
        var user = CreateUser(FsrFinanceOfficer);

        // Act
        var isInGroup = user.IsInGroup(KeycloakGroups.GetAccountingUnitGroup("fsr"));

        // Assert
        Assert.False(isInGroup);
    }

    [Fact]
    public void Validate_NoRequirements_DoesNotThrow()
    {
        // Arrange
        var builder = CreateUser().CreateClaimBuilder();

        // Act
        var exception = Record.Exception(builder.Validate);

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void MustBe_WithoutValidate_DoesNotThrow()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var exception = Record.Exception(() => user.CreateClaimBuilder().MustBe(AdministrationRole.HeadFinanceOfficer));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(AdministrationRole.HeadFinanceOfficer)]
    [InlineData(AdministrationRole.FinanceAdministrator)]
    public void MustBeOneOfAdministrationRoles_UserHasOneRole_DoesNotThrow(AdministrationRole role)
    {
        // Arrange
        var builder = CreateUser(KeycloakGroups.GetAdministrationRoleGroup(role)).CreateClaimBuilder()
            .MustBeOneOf(AdministrationRole.HeadFinanceOfficer, AdministrationRole.FinanceAdministrator);

        // Act
        var exception = Record.Exception(builder.Validate);

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void MustBeOneOfAdministrationRoles_UserHasNoneOfTheRoles_ThrowsWithAllGroups()
    {
        // Arrange
        var builder = CreateUser(FinanceReviewer).CreateClaimBuilder()
            .MustBeOneOf(AdministrationRole.HeadFinanceOfficer, AdministrationRole.FinanceAdministrator);

        // Act
        var exception = Assert.Throws<AccessForbiddenException>(builder.Validate);

        // Assert
        Assert.Contains(HeadFinanceOfficer, exception.Message);
        Assert.Contains(FinanceAdministrator, exception.Message);
    }

    [Fact]
    public void MustBeAllOfAdministrationRoles_UserHasAllRoles_DoesNotThrow()
    {
        // Arrange
        var builder = CreateUser(HeadFinanceOfficer, FinanceAdministrator).CreateClaimBuilder()
            .MustBeAllOf(AdministrationRole.HeadFinanceOfficer, AdministrationRole.FinanceAdministrator);

        // Act
        var exception = Record.Exception(builder.Validate);

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void MustBeAllOfAdministrationRoles_UserMissesOneRole_ThrowsWithMissingGroup()
    {
        // Arrange
        var builder = CreateUser(HeadFinanceOfficer).CreateClaimBuilder()
            .MustBeAllOf(AdministrationRole.HeadFinanceOfficer, AdministrationRole.FinanceAdministrator);

        // Act
        var exception = Assert.Throws<AccessForbiddenException>(builder.Validate);

        // Assert
        Assert.Contains(FinanceAdministrator, exception.Message);
        Assert.DoesNotContain(HeadFinanceOfficer, exception.Message);
    }

    [Fact]
    public void MustBeAccountingUnitRole_UserHasRoleInThisUnit_DoesNotThrow()
    {
        // Arrange
        var builder = CreateUser(FsrFinanceOfficer).CreateClaimBuilder()
            .ForAccountingUnit("fsr")
            .MustBe(AccountingUnitRole.FinanceOfficer);

        // Act
        var exception = Record.Exception(builder.Validate);

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void MustBeAccountingUnitRole_UserHasRoleInOtherUnit_Throws()
    {
        // Arrange
        var builder = CreateUser(SturaFinanceOfficer).CreateClaimBuilder()
            .ForAccountingUnit("fsr")
            .MustBe(AccountingUnitRole.FinanceOfficer);

        // Act & Assert
        Assert.Throws<AccessForbiddenException>(builder.Validate);
    }

    [Fact]
    public void MustBeAccountingUnitRole_NoAccountingUnit_Throws()
    {
        // Arrange
        var builder = CreateUser(FsrFinanceOfficer).CreateClaimBuilder()
            .MustBe(AccountingUnitRole.FinanceOfficer);

        // Act & Assert
        Assert.Throws<AccessForbiddenException>(builder.Validate);
    }

    [Fact]
    public void MustBeOneOfAccountingUnitRoles_UserHasOneRole_DoesNotThrow()
    {
        // Arrange
        var builder = CreateUser(FsrTreasurer).CreateClaimBuilder()
            .ForAccountingUnit("fsr")
            .MustBeOneOf(AccountingUnitRole.FinanceOfficer, AccountingUnitRole.Treasurer);

        // Act
        var exception = Record.Exception(builder.Validate);

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void MustBeAllOfAccountingUnitRoles_UserMissesOneRole_ThrowsWithMissingGroup()
    {
        // Arrange
        var builder = CreateUser(FsrTreasurer).CreateClaimBuilder()
            .ForAccountingUnit("fsr")
            .MustBeAllOf(AccountingUnitRole.FinanceOfficer, AccountingUnitRole.Treasurer);

        // Act
        var exception = Assert.Throws<AccessForbiddenException>(builder.Validate);

        // Assert
        Assert.Contains(FsrFinanceOfficer, exception.Message);
    }

    public static TheoryData<string[]> UsersWithAdministrationOrUnitRole => new()
    {
        new[] { HeadFinanceOfficer },
        new[] { FinanceAdministrator },
        new[] { FsrFinanceOfficer },
    };

    [Theory]
    [MemberData(nameof(UsersWithAdministrationOrUnitRole))]
    public void MustBeOneOfMixedRoles_UserHasAdministrationOrUnitRole_DoesNotThrow(string[] groupPaths)
    {
        // Arrange
        var builder = CreateUser(groupPaths).CreateClaimBuilder()
            .ForAccountingUnit("fsr")
            .MustBeOneOf([AdministrationRole.HeadFinanceOfficer, AdministrationRole.FinanceAdministrator], [AccountingUnitRole.FinanceOfficer]);

        // Act
        var exception = Record.Exception(builder.Validate);

        // Assert
        Assert.Null(exception);
    }

    public static TheoryData<string[]> UsersWithoutAdministrationOrUnitRole => new()
    {
        Array.Empty<string>(),
        new[] { FinanceReviewer },
        new[] { FsrTreasurer },
        new[] { SturaFinanceOfficer },
    };

    [Theory]
    [MemberData(nameof(UsersWithoutAdministrationOrUnitRole))]
    public void MustBeOneOfMixedRoles_UserHasNeitherRole_Throws(string[] groupPaths)
    {
        // Arrange
        var builder = CreateUser(groupPaths).CreateClaimBuilder()
            .ForAccountingUnit("fsr")
            .MustBeOneOf([AdministrationRole.HeadFinanceOfficer, AdministrationRole.FinanceAdministrator], [AccountingUnitRole.FinanceOfficer]);

        // Act & Assert
        Assert.Throws<AccessForbiddenException>(builder.Validate);
    }

    [Fact]
    public void Validate_SeveralRequirements_AllMustBeMet()
    {
        // Arrange
        var builder = CreateUser(HeadFinanceOfficer).CreateClaimBuilder()
            .ForAccountingUnit("fsr")
            .MustBe(AdministrationRole.HeadFinanceOfficer)
            .MustBe(AccountingUnitRole.Treasurer);

        // Act
        var exception = Assert.Throws<AccessForbiddenException>(builder.Validate);

        // Assert
        Assert.Contains(FsrTreasurer, exception.Message);
    }
}
