using eFinanzer.Features.GetUsers.Tasks.GetUser;

namespace eFinanzer.Features.GetUsers;

public static class RegistrationExtensions
{
    public static IServiceCollection AddGetUsers(this IServiceCollection services)
    {
        services.AddScoped<IGetUserTask, GetUserTask>();

        return services;
    }
}
