using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageFiscalYears.Errors;
using eFinanzer.Features.ManageFiscalYears.Tasks.UpdateFiscalYear.Errors;
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

        var current = await context.FiscalYears
            .Where(x => x.Key == command.Key && x.AccountingUnitKey == command.AccountingUnitKey)
            .Select(x => new { HasReceiptsOutside = x.Receipts!.Any(r => r.ReceiptAt < command.StartDate || r.ReceiptAt > command.EndDate) })
            .FirstOrDefaultAsync(ct);
        if (current is null)
            return new NotFoundError(nameof(FiscalYear), $"{command.AccountingUnitKey}/{command.Key}");

        if (current.HasReceiptsOutside)
            return new FiscalYearWithBookingsShortenedError(command.Key, command.AccountingUnitKey, command.StartDate, command.EndDate);

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
