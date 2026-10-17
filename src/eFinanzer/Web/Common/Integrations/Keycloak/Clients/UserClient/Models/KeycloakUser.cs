namespace eFinanzer.Common.Integrations.Keycloak.Clients.UserClient.Models;

/// <summary>
/// User as returned by the Keycloak admin REST API (UserRepresentation). <see cref="Id"/> is the <c>sub</c> claim of
/// the user's tokens.
/// </summary>
public record KeycloakUser
{
    public required string Id { get; init; }
    public required string Username { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public bool Enabled { get; init; }
}
