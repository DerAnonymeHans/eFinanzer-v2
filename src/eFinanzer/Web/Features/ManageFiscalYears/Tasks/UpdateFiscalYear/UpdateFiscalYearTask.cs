using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageFiscalYears.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.UpdateFiscalYear.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageFiscalYears.Tasks.UpdateFiscalYear;

public interface IUpdateFiscalYearTask
{
    Task<Result> Update(UpdateFiscalYearCommand command, CancellationToken ct);
}

public class UpdateFiscalYearTask(
    EFinanzerDbContext context,
    ILogger<UpdateFiscalYearTask> logger
    ) : IUpdateFiscalYearTask
{
    public async Task<Result> Update(UpdateFiscalYearCommand command, CancellationToken ct)
    {
        Result[] validations =
        [
            FiscalYearValidation.ValidateName(command.Name),
            FiscalYearValidation.ValidateDates(command.StartDate, command.EndDate),
        ];
        var failedValidation = validations.FirstOrDefault(x => x.IsError);
        if (failedValidation is not null)
            return failedValidation;

        var exists = await context.FiscalYears
            .AnyAsync(x => x.Key == command.Key && x.AccountingUnitKey == command.AccountingUnitKey, ct);
        if (!exists)
            return new NotFoundError(nameof(FiscalYear), $"{command.AccountingUnitKey}/{command.Key}");

        // The fiscal year itself is excluded, it may overlap its own old dates
        var overlappingFiscalYear = await context.FiscalYears
            .Where(x => x.AccountingUnitKey == command.AccountingUnitKey && x.Key != command.Key)
            .WhereOverlaps(command.StartDate, command.EndDate)
            .OrderBy(x => x.StartDate)
            .FirstOrDefaultAsync(ct);

        if (overlappingFiscalYear is not null)
            return new FiscalYearOverlapsExistingError(command.Key, command.AccountingUnitKey, command.StartDate,
                command.EndDate, overlappingFiscalYear.Key, overlappingFiscalYear.StartDate, overlappingFiscalYear.EndDate);

        var updatedRows = await context.FiscalYears
            .Where(x => x.Key == command.Key && x.AccountingUnitKey == command.AccountingUnitKey)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Name, command.Name)
                .SetProperty(x => x.StartDate, command.StartDate)
                .SetProperty(x => x.EndDate, command.EndDate), ct);

        if (updatedRows == 0)
            return new NotFoundError(nameof(FiscalYear), $"{command.AccountingUnitKey}/{command.Key}");

        logger.LogInformation("Updated fiscal year {FiscalYearKey} of accounting unit {AccountingUnitKey}",
            command.Key, command.AccountingUnitKey);
        return Result.Ok();
    }
}
