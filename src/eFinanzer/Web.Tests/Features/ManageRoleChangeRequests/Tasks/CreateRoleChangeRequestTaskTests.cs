using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageRoleChangeRequests;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.CreateRoleChangeRequest;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.CreateRoleChangeRequest.Models;
using eFinanzer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eFinanzer.Tests.Features.ManageRoleChangeRequests.Tasks;

public class CreateRoleChangeRequestTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private CreateRoleChangeRequestTask MyTask => new(
        Database.CreateContext(),
        new FixedTimeProvider(),
        NullLogger<CreateRoleChangeRequestTask>.Instance);

    private static CreateRoleChangeRequestRequest.RoleChange Change(string key, AccountingUnitRole role, bool isAdded = true)
        => new() { AccountingUnitKey = key, AccountingUnitRole = role, IsAdded = isAdded };

    private static CreateRoleChangeRequestRequest Request(params CreateRoleChangeRequestRequest.RoleChange[] roleChanges) => new()
    {
        RequestedBy = "alice",
        Comment = "Please",
        RoleChanges = roleChanges,
    };

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Context.AddRange(new AccountingUnit { Key = "fsr" }, new AccountingUnit { Key = "stura" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ValidRequest_StoresUnresolvedRequest()
    {
        // Arrange
        var request = Request(
            Change("fsr", AccountingUnitRole.Treasurer),
            Change("stura", AccountingUnitRole.FinanceOfficer, isAdded: false));

        // Act
        var result = await MyTask.Create(request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var stored = await Context.RoleChangeRequests.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("alice", stored.RequestedBy);
        Assert.Equal("Please", stored.RequestComment);
        Assert.Equal(FixedTimeProvider.DefaultNow, stored.CreatedAt);
        Assert.Null(stored.Resolution);
        Assert.Null(stored.ResolvedBy);
        Assert.Equal(
            [
                new RoleChangeRequest.RoleChange { AccountingUnitKey = "fsr", AccountingUnitRole = AccountingUnitRole.Treasurer, IsAdded = true },
                new RoleChangeRequest.RoleChange { AccountingUnitKey = "stura", AccountingUnitRole = AccountingUnitRole.FinanceOfficer, IsAdded = false },
            ],
            stored.RoleChanges);
    }

    [Fact]
    public async Task Create_UnknownAccountingUnit_ReturnsNotFound()
    {
        // Act
        var result = await MyTask.Create(Request(Change("fsr", AccountingUnitRole.Treasurer), Change("unknown", AccountingUnitRole.Treasurer)),
            CancellationToken.None);

        // Assert
        Assert.Equal(new NotFoundError(nameof(AccountingUnit), "unknown"), result.Error);
        Assert.False(await Context.RoleChangeRequests.AnyAsync(TestContext.Current.CancellationToken));
    }

    public static TheoryData<string, CreateRoleChangeRequestRequest> InvalidRequests => new()
    {
        { "no role changes", Request() },
        { "empty accounting unit key", Request(Change("", AccountingUnitRole.Treasurer)) },
        { "undefined role", Request(Change("fsr", (AccountingUnitRole)42)) },
        { "duplicate role", Request(Change("fsr", AccountingUnitRole.Treasurer), Change("fsr", AccountingUnitRole.Treasurer, isAdded: false)) },
        { "comment too long", Request(Change("fsr", AccountingUnitRole.Treasurer)) with { Comment = new string('x', RoleChangeRequestValidation.CommentMaxLength + 1) } },
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Create_InvalidRequest_ReturnsInvalidInput(string reason, CreateRoleChangeRequestRequest request)
    {
        // Act
        var result = await MyTask.Create(request, CancellationToken.None);

        // Assert
        Assert.True(result.Error is InvalidInputError, reason);
        Assert.False(await Context.RoleChangeRequests.AnyAsync(TestContext.Current.CancellationToken));
    }
}
