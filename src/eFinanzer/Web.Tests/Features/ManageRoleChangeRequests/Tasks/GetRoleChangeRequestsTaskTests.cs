using eFinanzer.Common.Pagination;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetRoleChangeRequests;
using eFinanzer.Tests.Infrastructure;
using static eFinanzer.Tests.Features.ManageRoleChangeRequests.RoleChangeRequestTestData;

namespace eFinanzer.Tests.Features.ManageRoleChangeRequests.Tasks;

public class GetRoleChangeRequestsTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private GetRoleChangeRequestsTask MyTask => new(Database.CreateContext());

    private static readonly DateTimeOffset Day1 = FixedTimeProvider.DefaultNow;
    private static readonly DateTimeOffset Day2 = Day1.AddDays(1);
    private static readonly DateTimeOffset Day3 = Day1.AddDays(2);

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Context.RoleChangeRequests.AddRange(
            Pending("alice", Day2),
            Resolved("alice", Day1),
            Pending("bob", Day1),
            Pending("alice", Day3));
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetPending_ReturnsUnresolvedRequestsOfAllUsersOldestFirst()
    {
        // Act
        var result = await MyTask.GetPending(new OffsetPagination(PageSize: 10), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal([("bob", Day1), ("alice", Day2), ("alice", Day3)],
            result.Value.Items.Select(x => (x.RequestedBy, x.CreatedAt)));
        Assert.Equal(3, result.Value.TotalCount);
        Assert.All(result.Value.Items, x => Assert.Null(x.Resolution));
    }

    [Fact]
    public async Task GetOwn_ReturnsAllRequestsOfTheUserNewestFirst()
    {
        // Act
        var result = await MyTask.GetOwn("alice", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal([Day3, Day2, Day1], result.Value.Select(x => x.CreatedAt));
        Assert.All(result.Value, x => Assert.Equal("alice", x.RequestedBy));
        Assert.NotNull(result.Value[2].Resolution);
    }

    [Fact]
    public async Task GetOwn_UserWithoutRequests_ReturnsEmptyList()
    {
        // Act
        var result = await MyTask.GetOwn("carol", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
