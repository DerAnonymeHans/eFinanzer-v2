using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit;

public interface ICreateAccountingUnitTask
{
    Task<Result> Create(CreateAccountingUnitCommand command, CancellationToken ct);
}

public class CreateAccountingUnitTask(
    EFinanzerDbContext context,
    ILogger<CreateAccountingUnitTask> logger
    ) : ICreateAccountingUnitTask
{
    public async Task<Result> Create(CreateAccountingUnitCommand command, CancellationToken ct)
    {
        var keyExists = await context.AccountingUnits.AnyAsync(x => x.Key == command.Key, ct);
        if (keyExists)
            return new InvalidInputError(nameof(command.Key), "Key already exists");

        var accountingUnit = new AccountingUnit
        {
            Key = command.Key,
            Description = command.Description,
            Name = command.Name,
        };
        context.AccountingUnits.Add(accountingUnit);
        await context.SaveChangesAsync(ct);

        return Result.Ok();
    }
}