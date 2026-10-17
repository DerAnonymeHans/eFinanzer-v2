namespace eFinanzer.Common.Integrations.Keycloak.Clients.UserClient.Models;

/// <summary>
/// Group as returned by the Keycloak admin REST API (GroupRepresentation)
/// </summary>
public record KeycloakGroup
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    /// <summary>
    /// Full group path, e.g. <c>/accounting-units/fsr/finance-officer</c>
    /// </summary>
    public required string Path { get; init; }
}
