namespace eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit.Models;

public record CreateAccountingUnitCommand
{
    public required string Key { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }

    /// <summary>
    /// Optional PNG, JPG or SVG logo of at most 5 MB
    /// </summary>
    public IFormFile? Logo { get; init; }
}