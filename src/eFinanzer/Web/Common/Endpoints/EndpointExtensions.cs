using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace eFinanzer.Common.Endpoints;

public static class EndpointExtensions
{
    /// <summary>
    /// Registers all non-abstract <see cref="IEndpoint"/> implementations of the assembly
    /// </summary>
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var endpoints = assembly.DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type));

        services.TryAddEnumerable(endpoints);
        return services;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        foreach (var endpoint in app.Services.GetRequiredService<IEnumerable<IEndpoint>>())
            endpoint.MapEndpoint(api);

        return app;
    }
}
