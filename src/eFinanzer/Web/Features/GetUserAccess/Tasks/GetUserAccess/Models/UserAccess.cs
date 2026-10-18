using eFinanzer.Common.Models;

namespace eFinanzer.Features.GetUserAccess.Tasks.GetUserAccess.Models;

public record UserAccess
{
    public required List<AccountingGroupDto> AccountingGroups { get; init; }
    public bool ManageAccountingUnits { get; init; }
    public required AdministrationRole[] AdministrationRoles { get; init; }

    public static UserAccess NoAccess() => new()
    {
        AccountingGroups = [],
        ManageAccountingUnits = false,
        AdministrationRoles = []
    };

    public record AccountingGroupDto
    {
        public required string Key { get; init; }
        public string? Name { get; init; }
        public bool ManageFiscalYears { get; init; }
        
        public required AccountingUnitRole[] AccountingUnitRoles { get; init; }
        public required FiscalYearDto[] FiscalYears { get; init; }
    }

    public record FiscalYearDto
    {
        public required string Key { get; init; }
        public string? Name { get; init; }
    }
}