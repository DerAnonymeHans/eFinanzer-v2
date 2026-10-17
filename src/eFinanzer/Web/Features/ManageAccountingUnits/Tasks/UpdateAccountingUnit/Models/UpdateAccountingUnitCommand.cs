namespace eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit.Models;

public record UpdateAccountingUnitCommand
{
    public required string Key { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
}
