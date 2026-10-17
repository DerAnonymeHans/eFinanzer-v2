using System.Net;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient.Models;
using eFinanzer.Common.Results.Errors;

namespace eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;

public interface IKeycloakUserClient
{
    /// <summary>
    /// Direct members of the group. Members of its subgroups are not included.
    /// </summary>
    /// <param name="groupPath">Full group path, e.g. <c>/accounting-units/fsr/finance-officer</c></param>
    Task<Result<List<KeycloakUser>>> GetUsersInGroup(string groupPath, CancellationToken ct);
    /// <summary>
    /// Direct subgroups of the group. Their own subgroups are not included.
    /// </summary>
    /// <param name="groupPath">Full group path, e.g. <c>/accounting-units</c></param>
    Task<Result<List<KeycloakGroup>>> GetSubgroups(string groupPath, CancellationToken ct);

    Task<Result<KeycloakUser>> GetUserByUsername(string username, CancellationToken ct);

    /// <summary>
    /// Makes the user a direct member of the group. Does nothing if the user already is a member.
    /// </summary>
    /// <param name="userId">Keycloak user id (<c>sub</c> claim)</param>
    /// <param name="groupPath">Full group path, e.g. <c>/accounting-units/fsr/finance-officer</c></param>
    Task<Result> AddUserToGroup(string userId, string groupPath, CancellationToken ct);

    /// <summary>
    /// Removes the user from the group. Does nothing if the user is no member.
    /// </summary>
    /// <param name="userId">Keycloak user id (<c>sub</c> claim)</param>
    /// <param name="groupPath">Full group path, e.g. <c>/accounting-units/fsr/finance-officer</c></param>
    Task<Result> RemoveUserFromGroup(string userId, string groupPath, CancellationToken ct);
}

/// <summary>
/// Typed client for the Keycloak admin REST API. The base address points to <c>/admin/realms/{realm}/</c> and the
/// service account token is added by <see cref="KeycloakAdminTokenHandler"/>.
/// </summary>
public class KeycloakUserClient(HttpClient httpClient) : IKeycloakUserClient
{
    private const int PageSize = 100;

    public async Task<Result<List<KeycloakUser>>> GetUsersInGroup(string groupPath, CancellationToken ct)
    {
        var groupResult = await GetGroupByPath(groupPath, ct);
        if (groupResult.IsError)
            return groupResult.Error;

        return await GetPaged<KeycloakUser>($"groups/{groupResult.Value.Id}/members?briefRepresentation=true", ct);
    }

    public async Task<Result<List<KeycloakGroup>>> GetSubgroups(string groupPath, CancellationToken ct)
    {
        var groupResult = await GetGroupByPath(groupPath, ct);
        if (groupResult.IsError)
            return groupResult.Error;

        return await GetPaged<KeycloakGroup>($"groups/{groupResult.Value.Id}/children?briefRepresentation=true", ct);
    }

    public async Task<Result<KeycloakUser>> GetUserByUsername(string username, CancellationToken ct)
    {
        var users = await httpClient.GetFromJsonAsync<List<KeycloakUser>>(
                        $"users?username={Uri.EscapeDataString(username)}&exact=true&briefRepresentation=true", ct)
                    ?? [];

        var user = users.FirstOrDefault();
        if (user is null)
            return new NotFoundError("User", username);

        return user;
    }

    public Task<Result> AddUserToGroup(string userId, string groupPath, CancellationToken ct)
        => ChangeGroupMembership(HttpMethod.Put, userId, groupPath, ct);

    public Task<Result> RemoveUserFromGroup(string userId, string groupPath, CancellationToken ct)
        => ChangeGroupMembership(HttpMethod.Delete, userId, groupPath, ct);

    private async Task<Result> ChangeGroupMembership(HttpMethod method, string userId, string groupPath,
        CancellationToken ct)
    {
        var groupResult = await GetGroupByPath(groupPath, ct);
        if (groupResult.IsError)
            return groupResult.Error;

        using var request = new HttpRequestMessage(method,
            $"users/{Uri.EscapeDataString(userId)}/groups/{groupResult.Value.Id}");
        using var response = await httpClient.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return new NotFoundError("User", userId);
        response.EnsureSuccessStatusCode();

        return Result.Ok();
    }

    private async Task<Result<KeycloakGroup>> GetGroupByPath(string groupPath, CancellationToken ct)
    {
        var escapedPath = string.Join('/', groupPath.Trim('/').Split('/').Select(Uri.EscapeDataString));

        using var response = await httpClient.GetAsync($"group-by-path/{escapedPath}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return new NotFoundError("Group", groupPath);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<KeycloakGroup>(ct)
               ?? throw new InvalidOperationException($"Keycloak returned an empty group for '{groupPath}'");
    }

    /// <summary>
    /// Loads all pages of a list endpoint that supports the <c>first</c> and <c>max</c> query parameters
    /// </summary>
    private async Task<List<T>> GetPaged<T>(string requestUri, CancellationToken ct)
    {
        var items = new List<T>();
        while (true)
        {
            var page = await httpClient.GetFromJsonAsync<List<T>>(
                           $"{requestUri}&first={items.Count}&max={PageSize}", ct)
                       ?? [];
            items.AddRange(page);

            if (page.Count < PageSize)
                return items;
        }
    }
}
