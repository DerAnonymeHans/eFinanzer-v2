using eFinanzer.Common.Models;

namespace eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit.Models;

public record GetAccountingUnitManagementDto
{
    public required string Key { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    
    public required UserDto[] UsersWithRoles { get; init; }

    public record UserDto
    {
        public required string UserName { get; init; }
        public required AccountingUnitRole[] Roles { get; init; }
    }
}
