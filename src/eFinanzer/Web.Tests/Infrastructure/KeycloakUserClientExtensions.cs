using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient.Models;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using NSubstitute;

namespace eFinanzer.Tests.Infrastructure;

/// <summary>
/// Arranges the Keycloak state seen by a substituted <see cref="IKeycloakUserClient"/>
/// </summary>
public static class KeycloakUserClientExtensions
{
    public static KeycloakUser User(string userName) => new()
    {
        Id = $"id-{userName}",
        Username = userName,
        Enabled = true,
    };

    public static KeycloakGroup RoleGroup(string key, AccountingUnitRole role) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = KeycloakGroups.GetGroupName(role),
        Path = KeycloakGroups.GetAccountingUnitRoleGroup(key, role),
    };

    /// <summary>
    /// Creates the role groups of the accounting unit with the given members
    /// </summary>
    public static void SetupRoleMembers(this IKeycloakUserClient client, string key,
        params (AccountingUnitRole Role, string[] UserNames)[] roles)
    {
        var groups = roles.Select(x => RoleGroup(key, x.Role)).ToList();
        client.GetSubgroups(KeycloakGroups.GetAccountingUnitGroup(key), Arg.Any<CancellationToken>())
            .Returns(Result<List<KeycloakGroup>>.Ok(groups));

        foreach (var (role, userNames) in roles)
        {
            client.GetUsersInGroup(KeycloakGroups.GetAccountingUnitRoleGroup(key, role), Arg.Any<CancellationToken>())
                .Returns(Result<List<KeycloakUser>>.Ok(userNames.Select(User).ToList()));
        }
    }

    /// <summary>
    /// Makes the users known to Keycloak and lets all group membership changes succeed
    /// </summary>
    public static void SetupUsers(this IKeycloakUserClient client, params string[] userNames)
    {
        foreach (var userName in userNames)
        {
            client.GetUserByUsername(userName, Arg.Any<CancellationToken>())
                .Returns(Result<KeycloakUser>.Ok(User(userName)));
        }

        client.AddUserToGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
        client.RemoveUserFromGroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok());
    }
}
