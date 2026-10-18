using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageFiscalYears.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.CreateFiscalYear.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.CreateFiscalYear.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageFiscalYears.Tasks.CreateFiscalYear;

public interface ICreateFiscalYearTask
{
    Task<Result> Create(CreateFiscalYearCommand command, CancellationToken ct);
}

public class CreateFiscalYearTask(
    EFinanzerDbContext context
    ) : ICreateFiscalYearTask
{
    public async Task<Result> Create(CreateFiscalYearCommand command, CancellationToken ct)
    {
        Result[] validations =
        [
            FiscalYearValidation.ValidateKey(command.Key),
            FiscalYearValidation.ValidateName(command.Name),
            FiscalYearValidation.ValidateDates(command.StartDate, command.EndDate),
        ];
        var failedValidation = validations.FirstOrDefault(x => x.IsError);
        if (failedValidation is not null)
            return failedValidation;

        var existsValidations = await context.AccountingUnits
            .Where(x => x.Key == command.AccountingUnitKey)
            .Select(x => new
            {
                FiscalYearAlreadyExists = x.FiscalYears!.Any(year => year.Key == command.Key),
            })
            .FirstOrDefaultAsync(ct);
        
        if (existsValidations is null)
            return new NotFoundError(nameof(AccountingUnit), command.AccountingUnitKey);
        
        if (existsValidations.FiscalYearAlreadyExists)
            return new FiscalYearAlreadyExistsError(command.Key, command.AccountingUnitKey);

        var overlappingFiscalYear = await context.FiscalYears
            .Where(x => x.AccountingUnitKey == command.AccountingUnitKey)
            .WhereOverlaps(command.StartDate, command.EndDate)
            .OrderBy(x => x.StartDate)
            .FirstOrDefaultAsync(ct);

        if (overlappingFiscalYear is not null)
            return new FiscalYearOverlapsExistingError(command.Key, command.AccountingUnitKey, command.StartDate,
                command.EndDate, overlappingFiscalYear.Key, overlappingFiscalYear.StartDate, overlappingFiscalYear.EndDate);
        
        var fiscalYear = new FiscalYear
        {
            AccountingUnitKey = command.AccountingUnitKey,
            Key = command.Key,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Name = command.Name,
        };
        context.FiscalYears.Add(fiscalYear);
        await context.SaveChangesAsync(ct);
        return Result.Ok();
    }
}