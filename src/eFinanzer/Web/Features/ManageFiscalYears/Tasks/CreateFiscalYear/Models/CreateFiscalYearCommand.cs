namespace eFinanzer.Features.ManageFiscalYears.Tasks.CreateFiscalYear.Models;

public record CreateFiscalYearCommand
{
    public required string Key { get; init; }
    public required string AccountingUnitKey { get; init; }
    public string? Name { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
}