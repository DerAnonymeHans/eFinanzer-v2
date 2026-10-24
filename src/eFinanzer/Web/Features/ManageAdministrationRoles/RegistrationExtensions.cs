using eFinanzer.Features.ManageAdministrationRoles.Tasks.GetAdministrationUsers;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.UpdateAdministrationRoles;

namespace eFinanzer.Features.ManageAdministrationRoles;

public static class RegistrationExtensions
{
    public static IServiceCollection AddManageAdministrationRoles(this IServiceCollection services)
    {
        services.AddScoped<IGetAdministrationUsersTask, GetAdministrationUsersTask>();
        services.AddScoped<IUpdateAdministrationRolesTask, UpdateAdministrationRolesTask>();

        return services;
    }
}
