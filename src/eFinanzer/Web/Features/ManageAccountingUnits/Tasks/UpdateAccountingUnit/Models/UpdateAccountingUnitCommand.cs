using eFinanzer.Common.Models;

namespace eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit.Models;

public record UpdateAccountingUnitCommand
{
    public required string Key { get; init; }
    public required string? Name { get; init; }
    public required string? Description { get; init; }

    /// <summary>
    /// Complete list of users and their roles in the accounting unit. Users that are not listed lose all their roles,
    /// so an empty list removes all users.
    /// </summary>
    public required List<UserRoles> UsersWithRoles { get; init; }

    public record UserRoles
    {
        public required string UserName { get; init; }
        public required AccountingUnitRole[] Roles { get; init; }
    }
}
