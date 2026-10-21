using eFinanzer.Features.ManageRoleChangeRequests.Tasks.CreateRoleChangeRequest;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.DeleteRoleChangeRequest;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetAccountingUnits;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetRoleChangeRequests;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.ResolveRoleChangeRequest;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace eFinanzer.Features.ManageRoleChangeRequests;

public static class RegistrationExtensions
{
    public static IServiceCollection AddManageRoleChangeRequests(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<ICreateRoleChangeRequestTask, CreateRoleChangeRequestTask>();
        services.AddScoped<IGetRoleChangeRequestsTask, GetRoleChangeRequestsTask>();
        services.AddScoped<IResolveRoleChangeRequestTask, ResolveRoleChangeRequestTask>();
        services.AddScoped<IDeleteRoleChangeRequestTask, DeleteRoleChangeRequestTask>();
        services.AddScoped<IGetAccountingUnitsTask, GetAccountingUnitsTask>();

        return services;
    }
}
