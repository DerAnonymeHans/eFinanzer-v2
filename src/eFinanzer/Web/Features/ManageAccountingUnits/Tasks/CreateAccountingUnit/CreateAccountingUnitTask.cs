using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit.Errors;
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
        Result[] validations =
        [
            AccountingUnitValidation.ValidateKey(command.Key),
            AccountingUnitValidation.ValidateName(command.Name),
            AccountingUnitValidation.ValidateDescription(command.Description),
        ];
        var failedValidation = validations.FirstOrDefault(x => x.IsError);
        if (failedValidation is not null)
            return failedValidation;

        var keyExists = await context.AccountingUnits.AnyAsync(x => x.Key == command.Key, ct);
        if (keyExists)
            return new AccountingUnitKeyAlreadyExistsError(command.Key);

        var accountingUnit = new AccountingUnit
        {
            Key = command.Key,
            Description = command.Description,
            Name = command.Name,
        };
        context.AccountingUnits.Add(accountingUnit);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created accounting unit {AccountingUnitKey}", command.Key);
        return Result.Ok();
    }
}
