using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.DeleteRoleChangeRequest;
using eFinanzer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using static eFinanzer.Tests.Features.ManageRoleChangeRequests.RoleChangeRequestTestData;

namespace eFinanzer.Tests.Features.ManageRoleChangeRequests.Tasks;

public class DeleteRoleChangeRequestTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private DeleteRoleChangeRequestTask MyTask => new(Database.CreateContext(), NullLogger<DeleteRoleChangeRequestTask>.Instance);

    private async Task<int> Add(RoleChangeRequest request)
    {
        Context.RoleChangeRequests.Add(request);
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return request.Id;
    }

    [Fact]
    public async Task Delete_OwnPendingRequest_RemovesOnlyThisRequest()
    {
        // Arrange
        var id = await Add(Pending("alice", FixedTimeProvider.DefaultNow));
        var otherId = await Add(Pending("alice", FixedTimeProvider.DefaultNow));

        // Act
        var result = await MyTask.Delete(id, "alice", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal([otherId], await Context.RoleChangeRequests.Select(x => x.Id).ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Delete_OwnResolvedRequest_ReturnsConflictAndKeepsRequest()
    {
        // Arrange
        var id = await Add(Resolved("alice", FixedTimeProvider.DefaultNow));

        // Act
        var result = await MyTask.Delete(id, "alice", CancellationToken.None);

        // Assert
        Assert.Equal(new RoleChangeRequestAlreadyResolvedError(id), result.Error);
        Assert.True(await Context.RoleChangeRequests.AnyAsync(x => x.Id == id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Delete_RequestOfOtherUser_ReturnsNotFoundAndKeepsRequest()
    {
        // Arrange
        var id = await Add(Pending("bob", FixedTimeProvider.DefaultNow));

        // Act
        var result = await MyTask.Delete(id, "alice", CancellationToken.None);

        // Assert
        Assert.Equal(new NotFoundError(nameof(RoleChangeRequest), id.ToString()), result.Error);
        Assert.True(await Context.RoleChangeRequests.AnyAsync(x => x.Id == id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        // Act
        var result = await MyTask.Delete(42, "alice", CancellationToken.None);

        // Assert
        Assert.Equal(new NotFoundError(nameof(RoleChangeRequest), "42"), result.Error);
    }
}
