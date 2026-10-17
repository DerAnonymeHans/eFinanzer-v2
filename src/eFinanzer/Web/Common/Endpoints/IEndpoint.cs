namespace eFinanzer.Common.Endpoints;

/// <summary>
/// A single HTTP endpoint. Implementations are discovered by <see cref="EndpointExtensions.AddEndpoints"/> and mapped
/// by <see cref="EndpointExtensions.MapEndpoints"/>.
/// </summary>
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
