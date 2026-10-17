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

    /// <summary>
    /// Id of the group with the given path. Derived from the path, so tests can refer to it without arranging it.
    /// </summary>
    public static string GroupId(string groupPath) => $"id-{groupPath}";

    public static KeycloakGroup RoleGroup(string key, AccountingUnitRole role) => Group(KeycloakGroups.GetAccountingUnitRoleGroup(key, role));

    public static KeycloakGroup Group(string groupPath) => new()
    {
        Id = GroupId(groupPath),
        Name = groupPath.Split('/')[^1],
        Path = groupPath,
    };

    /// <summary>
    /// Makes the groups resolvable by their path
    /// </summary>
    public static void SetupGroups(this IKeycloakUserClient client, params string[] groupPaths)
    {
        foreach (var groupPath in groupPaths)
        {
            client.GetGroupIdFromPath(groupPath, Arg.Any<CancellationToken>())
                .Returns(Result<string>.Ok(GroupId(groupPath)));
        }
    }

    /// <summary>
    /// Makes the given groups the subgroups of the group of the accounting unit
    /// </summary>
    public static void SetupSubgroups(this IKeycloakUserClient client, string key, params KeycloakGroup[] subgroups)
    {
        var groupPath = KeycloakGroups.GetAccountingUnitGroup(key);
        client.SetupGroups(groupPath);
        client.GetSubgroups(GroupId(groupPath), Arg.Any<CancellationToken>())
            .Returns(Result<List<KeycloakGroup>>.Ok(subgroups.ToList()));
    }

    /// <summary>
    /// Creates the group of the accounting unit and all its role groups, the given roles with the given members
    /// </summary>
    public static void SetupRoleMembers(this IKeycloakUserClient client, string key,
        params (AccountingUnitRole Role, string[] UserNames)[] roles)
    {
        client.SetupSubgroups(key, roles.Select(x => RoleGroup(key, x.Role)).ToArray());
        client.SetupGroups(Enum.GetValues<AccountingUnitRole>()
            .Select(role => KeycloakGroups.GetAccountingUnitRoleGroup(key, role))
            .ToArray());

        foreach (var (role, userNames) in roles)
        {
            client.GetUsersInGroup(RoleGroup(key, role).Id, Arg.Any<CancellationToken>())
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

    /// <summary>
    /// Makes the <c>/accounting-units</c> group exist and lets creating subgroups succeed. The id of a new group is
    /// <see cref="GroupId"/> of its path.
    /// </summary>
    public static void SetupGroupCreation(this IKeycloakUserClient client)
    {
        client.SetupGroups(KeycloakGroups.AccountingUnitsGroup);
        client.CreateSubgroup(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<string>.Ok($"{call.ArgAt<string>(0)}/{call.ArgAt<string>(1)}"));
    }
}
