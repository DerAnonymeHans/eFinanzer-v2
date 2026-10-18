namespace eFinanzer.Features.ManageFiscalYears.Tasks.GetFiscalYears.Models;

public record GetFiscalYearManagementListDto
{
    public required string Key { get; init; }
    public required string AccountingUnitKey { get; init; }
    public string? Name { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required int ReceiptsCount { get; init; }
    public required int BudgetsCount { get; init; }
}
