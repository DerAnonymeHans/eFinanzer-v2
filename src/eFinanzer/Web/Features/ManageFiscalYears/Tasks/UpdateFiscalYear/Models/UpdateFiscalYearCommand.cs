namespace eFinanzer.Features.ManageFiscalYears.Tasks.UpdateFiscalYear.Models;

public record UpdateFiscalYearCommand
{
    public required string Key { get; init; }
    public required string AccountingUnitKey { get; init; }
    public required string? Name { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
}
