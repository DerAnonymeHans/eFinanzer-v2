using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.GetUsers.Tasks.GetUser.Models;
using Microsoft.Extensions.Caching.Memory;

namespace eFinanzer.Features.GetUsers.Tasks.GetUser;

public interface IGetUserTask
{
    Task<Result<GetUserDto>> Get(string username, CancellationToken ct);

    Task<Result<List<GetUserDto>>> Search(string? searchString, CancellationToken ct);
}

public class GetUserTask(
    IKeycloakUserClient keycloakUserClient,
    IMemoryCache cache,
    ILogger<GetUserTask> logger
    ) : IGetUserTask
{
    public const int SearchStringMinLength = 2;
    public const int SearchStringMaxLength = 100;

    /// <summary>
    /// Loads the user from Keycloak once and caches it indefinitely. Errors (e.g. unknown users) are not cached, so a
    /// user created later is found on the next call.
    /// </summary>
    public async Task<Result<GetUserDto>> Get(string username, CancellationToken ct)
    {
        var cacheKey = GetCacheKey(username);
        if (cache.TryGetValue(cacheKey, out GetUserDto? cachedUser) && cachedUser is not null)
            return cachedUser;

        var getUserResult = await keycloakUserClient.GetUserByUsername(username, ct);
        if (getUserResult.IsError)
        {
            logger.LogWarning("Loading user {Username} from Keycloak failed: {Error}", username, getUserResult.Error);
            return getUserResult.Error;
        }

        var user = new GetUserDto
        {
            Username = getUserResult.Value.Username,
            FirstName = getUserResult.Value.FirstName,
            LastName = getUserResult.Value.LastName,
        };

        cache.Set(cacheKey, user, new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });

        return user;
    }

    /// <summary>
    /// Finds users by their real name, username or email, see <see cref="IKeycloakUserClient.SearchUser"/>. Sorted by
    /// last and first name, so the results can be shown as they are. Not cached, new users should be found right away.
    /// </summary>
    public async Task<Result<List<GetUserDto>>> Search(string? searchString, CancellationToken ct)
    {
        var trimmed = searchString?.Trim() ?? "";
        if (trimmed.Length < SearchStringMinLength || trimmed.Length > SearchStringMaxLength)
            return new InvalidInputError("Search",
                $"Search must be between {SearchStringMinLength} and {SearchStringMaxLength} characters long");

        var searchResult = await keycloakUserClient.SearchUser(trimmed, ct);
        if (searchResult.IsError)
        {
            logger.LogWarning("Searching users for {SearchString} in Keycloak failed: {Error}", trimmed, searchResult.Error);
            return searchResult.Error;
        }

        return searchResult.Value
            .OrderBy(x => x.LastName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => x.FirstName, StringComparer.CurrentCultureIgnoreCase)
            .Select(x => new GetUserDto
            {
                Username = x.Username,
                FirstName = x.FirstName,
                LastName = x.LastName,
            })
            .ToList();
    }

    private static string GetCacheKey(string username) => $"{nameof(GetUserTask)}:{username}";
}
