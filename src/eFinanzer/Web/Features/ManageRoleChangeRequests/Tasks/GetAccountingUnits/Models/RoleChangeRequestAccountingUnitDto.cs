namespace eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetAccountingUnits.Models;

/// <summary>
/// Accounting unit a role can be requested in, without the details only managers may see
/// </summary>
public record RoleChangeRequestAccountingUnitDto
{
    public required string Key { get; init; }
    public string? Name { get; init; }
}
