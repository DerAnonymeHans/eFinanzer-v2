using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Features.GetUsers.Tasks.GetUser.Models;
using Microsoft.Extensions.Caching.Memory;

namespace eFinanzer.Features.GetUsers.Tasks.GetUser;

public interface IGetUserTask
{
    Task<Result<GetUserDto>> Get(string username, CancellationToken ct);
}

public class GetUserTask(
    IKeycloakUserClient keycloakUserClient,
    IMemoryCache cache,
    ILogger<GetUserTask> logger
    ) : IGetUserTask
{
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

    private static string GetCacheKey(string username) => $"{nameof(GetUserTask)}:{username}";
}
