namespace eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit.Models;

public record AccountingUnitModel
{
    public required string Key { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
}
