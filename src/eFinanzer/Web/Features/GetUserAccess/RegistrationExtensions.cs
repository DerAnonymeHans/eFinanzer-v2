using eFinanzer.Features.GetUserAccess.Tasks.GetUserAccess;

namespace eFinanzer.Features.GetUserAccess;

public static class RegistrationExtensions
{
    public static IServiceCollection AddGetUserAccess(this IServiceCollection services)
    {
        services.AddScoped<IGetUserAccessTask, GetUserAccessTask>();

        return services;
    }
}
