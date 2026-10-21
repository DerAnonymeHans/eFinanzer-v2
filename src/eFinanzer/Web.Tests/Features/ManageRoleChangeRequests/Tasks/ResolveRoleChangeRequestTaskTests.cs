using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageRoleChangeRequests;
using eFinanzer.Features.ManageRoleChangeRequests.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.ResolveRoleChangeRequest;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.ResolveRoleChangeRequest.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.ResolveRoleChangeRequest.Models;
using eFinanzer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static eFinanzer.Tests.Features.ManageRoleChangeRequests.RoleChangeRequestTestData;

namespace eFinanzer.Tests.Features.ManageRoleChangeRequests.Tasks;

public class ResolveRoleChangeRequestTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private static readonly DateTimeOffset ResolvedAt = FixedTimeProvider.DefaultNow.AddDays(3);

    private readonly IKeycloakUserClient _keycloakUserClient = Substitute.For<IKeycloakUserClient>();

    private ResolveRoleChangeRequestTask MyTask => new(
        Database.CreateContext(),
        _keycloakUserClient,
        new FixedTimeProvider(ResolvedAt),
        NullLogger<ResolveRoleChangeRequestTask>.Instance);

    private static string RoleGroupId(string key, AccountingUnitRole role) => KeycloakUserClientExtensions.RoleGroup(key, role).Id;

    private static ResolveRoleChangeRequestCommand Command(int id, RoleChangeRequest.ResolutionStatus resolution) => new()
    {
        Id = id,
        ResolvedBy = "admin",
        Resolution = resolution,
        Comment = "Done",
    };

    private async Task<int> Add(RoleChangeRequest request)
    {
        Context.RoleChangeRequests.Add(request);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return request.Id;
    }

    private async Task<RoleChangeRequest> Load(int id)
        => await Context.RoleChangeRequests.SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);

    private void SetupRoleGroups(params (string Key, AccountingUnitRole Role)[] roles)
        => _keycloakUserClient.SetupGroups(roles.Select(x => KeycloakGroups.GetAccountingUnitRoleGroup(x.Key, x.Role)).ToArray());

    [Fact]
    public async Task Resolve_Approved_AppliesRoleChangesWithRemovalsFirstAndMarksResolved()
    {
        // Arrange
        var id = await Add(Pending("alice", FixedTimeProvider.DefaultNow,
            Change("fsr", AccountingUnitRole.Treasurer),
            Change("stura", AccountingUnitRole.FinanceOfficer, isAdded: false)));
        _keycloakUserClient.SetupUsers("alice");
        SetupRoleGroups(("fsr", AccountingUnitRole.Treasurer), ("stura", AccountingUnitRole.FinanceOfficer));

        // Act
        var result = await MyTask.Resolve(Command(id, RoleChangeRequest.ResolutionStatus.Approved), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Received.InOrder(() =>
        {
            _keycloakUserClient.RemoveUserFromGroup("id-alice", RoleGroupId("stura", AccountingUnitRole.FinanceOfficer), Arg.Any<CancellationToken>());
            _keycloakUserClient.AddUserToGroup("id-alice", RoleGroupId("fsr", AccountingUnitRole.Treasurer), Arg.Any<CancellationToken>());
        });
        var stored = await Load(id);
        Assert.Equal(RoleChangeRequest.ResolutionStatus.Approved, stored.Resolution);
        Assert.Equal("admin", stored.ResolvedBy);
        Assert.Equal(ResolvedAt, stored.ResolvedAt);
        Assert.Equal("Done", stored.ResolutionComment);
    }

    [Fact]
    public async Task Resolve_Dismissed_MarksResolvedWithoutChangingRoles()
    {
        // Arrange
        var id = await Add(Pending("alice", FixedTimeProvider.DefaultNow));

        // Act
        var result = await MyTask.Resolve(Command(id, RoleChangeRequest.ResolutionStatus.Dismissed), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(RoleChangeRequest.ResolutionStatus.Dismissed, (await Load(id)).Resolution);
        await _keycloakUserClient.DidNotReceive().GetUserByUsername(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _keycloakUserClient.DidNotReceive().AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolve_UnknownId_ReturnsNotFound()
    {
        // Act
        var result = await MyTask.Resolve(Command(42, RoleChangeRequest.ResolutionStatus.Dismissed), CancellationToken.None);

        // Assert
        Assert.Equal(new NotFoundError(nameof(RoleChangeRequest), "42"), result.Error);
    }

    [Fact]
    public async Task Resolve_AlreadyResolved_ReturnsConflictWithoutChangingRoles()
    {
        // Arrange
        var id = await Add(Resolved("alice", FixedTimeProvider.DefaultNow));
        _keycloakUserClient.SetupUsers("alice");
        SetupRoleGroups(("fsr", AccountingUnitRole.Treasurer));

        // Act
        var result = await MyTask.Resolve(Command(id, RoleChangeRequest.ResolutionStatus.Approved), CancellationToken.None);

        // Assert
        Assert.Equal(new RoleChangeRequestAlreadyResolvedError(id), result.Error);
        Assert.Equal(RoleChangeRequest.ResolutionStatus.Dismissed, (await Load(id)).Resolution);
        await _keycloakUserClient.DidNotReceive().AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolve_ApprovedWithUnknownRequester_ReturnsNotApplicableAndStaysPending()
    {
        // Arrange
        var id = await Add(Pending("alice", FixedTimeProvider.DefaultNow));
        _keycloakUserClient.GetUserByUsername("alice", Arg.Any<CancellationToken>())
            .Returns(Result<KeycloakUser>.Fail(new NotFoundError("User", "alice")));

        // Act
        var result = await MyTask.Resolve(Command(id, RoleChangeRequest.ResolutionStatus.Approved), CancellationToken.None);

        // Assert
        Assert.IsType<RoleChangeRequestNotApplicableError>(result.Error);
        Assert.Null((await Load(id)).Resolution);
    }

    [Fact]
    public async Task Resolve_ApprovedWithMissingRoleGroup_ReturnsNotApplicableWithoutChangingRoles()
    {
        // Arrange
        var id = await Add(Pending("alice", FixedTimeProvider.DefaultNow,
            Change("fsr", AccountingUnitRole.Treasurer),
            Change("deleted", AccountingUnitRole.Treasurer)));
        _keycloakUserClient.SetupUsers("alice");
        SetupRoleGroups(("fsr", AccountingUnitRole.Treasurer));
        var missingGroup = KeycloakGroups.GetAccountingUnitRoleGroup("deleted", AccountingUnitRole.Treasurer);
        _keycloakUserClient.GetGroupIdFromPath(missingGroup, Arg.Any<CancellationToken>())
            .Returns(Result<string>.Fail(new NotFoundError("Group", missingGroup)));

        // Act
        var result = await MyTask.Resolve(Command(id, RoleChangeRequest.ResolutionStatus.Approved), CancellationToken.None);

        // Assert
        Assert.IsType<RoleChangeRequestNotApplicableError>(result.Error);
        Assert.Null((await Load(id)).Resolution);
        await _keycloakUserClient.DidNotReceive().AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolve_ApplyingRoleChangeFails_ReturnsErrorAndStaysPending()
    {
        // Arrange
        var id = await Add(Pending("alice", FixedTimeProvider.DefaultNow, Change("fsr", AccountingUnitRole.Treasurer)));
        _keycloakUserClient.SetupUsers("alice");
        SetupRoleGroups(("fsr", AccountingUnitRole.Treasurer));
        _keycloakUserClient.AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(new NotFoundError("User or group", "x")));

        // Act
        var result = await MyTask.Resolve(Command(id, RoleChangeRequest.ResolutionStatus.Approved), CancellationToken.None);

        // Assert
        Assert.IsType<ApplyRoleChangeError>(result.Error);
        var stored = await Load(id);
        Assert.Null(stored.Resolution);
        Assert.Null(stored.ResolvedBy);
        Assert.Null(stored.ResolvedAt);
        Assert.Null(stored.ResolutionComment);
    }

    [Fact]
    public async Task Resolve_CommentTooLong_ReturnsInvalidInput()
    {
        // Arrange
        var id = await Add(Pending("alice", FixedTimeProvider.DefaultNow));
        var command = Command(id, RoleChangeRequest.ResolutionStatus.Dismissed) with
        {
            Comment = new string('x', RoleChangeRequestValidation.CommentMaxLength + 1),
        };

        // Act
        var result = await MyTask.Resolve(command, CancellationToken.None);

        // Assert
        Assert.IsType<InvalidInputError>(result.Error);
        Assert.Null((await Load(id)).Resolution);
    }
}
