using System.Net;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient.Models;
using eFinanzer.Common.Results.Errors;

namespace eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;

public interface IKeycloakUserClient
{
    /// <summary>
    /// Resolves a group path to the group id, which all other group methods expect
    /// </summary>
    /// <param name="groupPath">Full group path, e.g. <c>/accounting-units/fsr/finance-officer</c></param>
    Task<Result<string>> GetGroupIdFromPath(string groupPath, CancellationToken ct);

    /// <summary>
    /// Direct members of the group. Members of its subgroups are not included.
    /// </summary>
    /// <param name="groupId">Keycloak group id</param>
    Task<Result<List<KeycloakUser>>> GetUsersInGroup(string groupId, CancellationToken ct);

    /// <summary>
    /// Direct subgroups of the group. Their own subgroups are not included.
    /// </summary>
    /// <param name="groupId">Keycloak group id</param>
    Task<Result<List<KeycloakGroup>>> GetSubgroups(string groupId, CancellationToken ct);

    /// <summary>
    /// Creates a direct subgroup of the group
    /// </summary>
    /// <param name="parentGroupId">Keycloak group id of the parent</param>
    /// <param name="name">Name of the new group, e.g. <c>finance-officer</c></param>
    /// <returns>Id of the new group, or an <see cref="AlreadyExistsError"/> if the parent already has a subgroup with
    /// this name</returns>
    Task<Result<string>> CreateSubgroup(string parentGroupId, string name, CancellationToken ct);

    /// <summary>
    /// Deletes the group including all its subgroups. Its members lose their membership.
    /// </summary>
    /// <param name="groupId">Keycloak group id</param>
    Task<Result> DeleteGroup(string groupId, CancellationToken ct);

    Task<Result<KeycloakUser>> GetUserByUsername(string username, CancellationToken ct);

    /// <summary>
    /// Makes the user a direct member of the group. Does nothing if the user already is a member.
    /// </summary>
    /// <param name="userId">Keycloak user id (<c>sub</c> claim)</param>
    /// <param name="groupId">Keycloak group id</param>
    Task<Result> AddUserToGroup(string userId, string groupId, CancellationToken ct);

    /// <summary>
    /// Removes the user from the group. Does nothing if the user is no member.
    /// </summary>
    /// <param name="userId">Keycloak user id (<c>sub</c> claim)</param>
    /// <param name="groupId">Keycloak group id</param>
    Task<Result> RemoveUserFromGroup(string userId, string groupId, CancellationToken ct);
}

/// <summary>
/// Typed client for the Keycloak admin REST API. The base address points to <c>/admin/realms/{realm}/</c> and the
/// service account token is added by <see cref="KeycloakAdminTokenHandler"/>.
/// </summary>
public class KeycloakUserClient(HttpClient httpClient) : IKeycloakUserClient
{
    private const int PageSize = 100;

    public async Task<Result<string>> GetGroupIdFromPath(string groupPath, CancellationToken ct)
    {
        var escapedPath = string.Join('/', groupPath.Trim('/').Split('/').Select(Uri.EscapeDataString));

        using var response = await httpClient.GetAsync($"group-by-path/{escapedPath}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return new NotFoundError("Group", groupPath);
        response.EnsureSuccessStatusCode();

        var group = await response.Content.ReadFromJsonAsync<KeycloakGroup>(ct)
                    ?? throw new InvalidOperationException($"Keycloak returned an empty group for '{groupPath}'");
        return group.Id;
    }

    public Task<Result<List<KeycloakUser>>> GetUsersInGroup(string groupId, CancellationToken ct)
        => GetPaged<KeycloakUser>($"groups/{Uri.EscapeDataString(groupId)}/members?briefRepresentation=true", groupId, ct);

    public Task<Result<List<KeycloakGroup>>> GetSubgroups(string groupId, CancellationToken ct)
        => GetPaged<KeycloakGroup>($"groups/{Uri.EscapeDataString(groupId)}/children?briefRepresentation=true", groupId, ct);

    public async Task<Result<string>> CreateSubgroup(string parentGroupId, string name, CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"groups/{Uri.EscapeDataString(parentGroupId)}/children", new { name }, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return new NotFoundError("Group", parentGroupId);
        if (response.StatusCode == HttpStatusCode.Conflict)
            return new AlreadyExistsError("Group", name);
        response.EnsureSuccessStatusCode();

        // Keycloak only returns the id of the new group as last segment of the Location header
        var location = response.Headers.Location
                       ?? throw new InvalidOperationException($"Keycloak returned no location for the new group '{name}'");
        return location.OriginalString.TrimEnd('/').Split('/')[^1];
    }

    public async Task<Result> DeleteGroup(string groupId, CancellationToken ct)
    {
        using var response = await httpClient.DeleteAsync($"groups/{Uri.EscapeDataString(groupId)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return new NotFoundError("Group", groupId);
        response.EnsureSuccessStatusCode();

        return Result.Ok();
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

    public Task<Result> AddUserToGroup(string userId, string groupId, CancellationToken ct)
        => ChangeGroupMembership(HttpMethod.Put, userId, groupId, ct);

    public Task<Result> RemoveUserFromGroup(string userId, string groupId, CancellationToken ct)
        => ChangeGroupMembership(HttpMethod.Delete, userId, groupId, ct);

    private async Task<Result> ChangeGroupMembership(HttpMethod method, string userId, string groupId,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method,
            $"users/{Uri.EscapeDataString(userId)}/groups/{Uri.EscapeDataString(groupId)}");
        using var response = await httpClient.SendAsync(request, ct);
        // Keycloak does not tell whether the user or the group is missing
        if (response.StatusCode == HttpStatusCode.NotFound)
            return new NotFoundError("User or group", $"{userId}/{groupId}");
        response.EnsureSuccessStatusCode();

        return Result.Ok();
    }

    /// <summary>
    /// Loads all pages of a list endpoint of a group that supports the <c>first</c> and <c>max</c> query parameters
    /// </summary>
    private async Task<Result<List<T>>> GetPaged<T>(string requestUri, string groupId, CancellationToken ct)
    {
        var items = new List<T>();
        while (true)
        {
            using var response = await httpClient.GetAsync($"{requestUri}&first={items.Count}&max={PageSize}", ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new NotFoundError("Group", groupId);
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<List<T>>(ct) ?? [];
            items.AddRange(page);

            if (page.Count < PageSize)
                return items;
        }
    }
}
