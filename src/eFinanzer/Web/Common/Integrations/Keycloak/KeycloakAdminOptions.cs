using System.ComponentModel.DataAnnotations;

namespace eFinanzer.Common.Integrations.Keycloak;

/// <summary>
/// Confidential Keycloak client with a service account, used to call the admin REST API
/// </summary>
public class KeycloakAdminOptions
{
    public const string SectionName = "KeycloakAdmin";

    /// <summary>
    /// Base URL of Keycloak including the relative path, e.g. <c>https://efinanzer.stura.htwk-leipzig.de/auth</c>
    /// </summary>
    [Required]
    public string BaseUrl { get; set; } = null!;

    [Required]
    public string Realm { get; set; } = null!;

    [Required]
    public string ClientId { get; set; } = null!;

    [Required]
    public string ClientSecret { get; set; } = null!;

    public Uri TokenEndpoint => new($"{BaseUrl.TrimEnd('/')}/realms/{Realm}/protocol/openid-connect/token");

    public Uri AdminApiBaseAddress => new($"{BaseUrl.TrimEnd('/')}/admin/realms/{Realm}/");
}
