using eFinanzer.Common.Models;

namespace eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit.Models;

public record UpdateAccountingUnitCommand
{
    public required string Key { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }

    /// <summary>
    /// Complete list of users and their roles in the accounting unit. Users that are not listed lose all their roles.
    /// <c>null</c> leaves the roles unchanged.
    /// </summary>
    public List<UserRoles>? UsersWithRoles { get; init; }

    public record UserRoles
    {
        public required string UserName { get; init; }
        public required AccountingUnitRole[] Roles { get; init; }
    }
}
