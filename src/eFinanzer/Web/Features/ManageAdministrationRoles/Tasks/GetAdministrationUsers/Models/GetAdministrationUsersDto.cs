using eFinanzer.Common.Models;

namespace eFinanzer.Features.ManageAdministrationRoles.Tasks.GetAdministrationUsers.Models;

public record GetAdministrationUsersDto
{
    public required AdministrationUserDto[] AdministrationUsers { get; init; }
}

public record AdministrationUserDto
{
    public required string Username { get; init; }
    public required AdministrationRole[] Roles { get; init; }
}