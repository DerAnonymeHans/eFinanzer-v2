using eFinanzer.Common.Database.Models;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetAccountingUnits;
using eFinanzer.Tests.Infrastructure;

namespace eFinanzer.Tests.Features.ManageRoleChangeRequests.Tasks;

public class GetAccountingUnitsTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private GetAccountingUnitsTask MyTask => new(Database.CreateContext());

    [Fact]
    public async Task GetAll_ReturnsAllAccountingUnitsOrderedByKey()
    {
        // Arrange
        Context.AccountingUnits.AddRange(
            new AccountingUnit { Key = "stura", Name = "StuRa", Description = "Not part of the result" },
            new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await MyTask.GetAll(CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal([("fsr", null), ("stura", "StuRa")], result.Value.Select(x => (x.Key, x.Name)));
    }

    [Fact]
    public async Task GetAll_NoAccountingUnits_ReturnsEmptyList()
    {
        // Act
        var result = await MyTask.GetAll(CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
