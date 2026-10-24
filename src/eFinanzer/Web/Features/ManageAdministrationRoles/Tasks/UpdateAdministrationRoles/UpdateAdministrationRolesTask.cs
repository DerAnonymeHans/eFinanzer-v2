using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Common.Tasks.GetRoleChanges;
using eFinanzer.Common.Tasks.GetRoleChanges.Models;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.GetAdministrationUsers;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.UpdateAdministrationRoles.Errors;
using eFinanzer.Features.ManageAdministrationRoles.Tasks.UpdateAdministrationRoles.Models;

namespace eFinanzer.Features.ManageAdministrationRoles.Tasks.UpdateAdministrationRoles;

public interface IUpdateAdministrationRolesTask
{
    Task<Result> Update(UpdateAdministrationRolesCommand command, CancellationToken ct);
}

public class UpdateAdministrationRolesTask(
    IGetAdministrationUsersTask getAdministrationUsersTask,
    IHandleRoleChangesTask handleRoleChangesTask,
    ILogger<UpdateAdministrationRolesTask> logger
    ) : IUpdateAdministrationRolesTask
{
    public async Task<Result> Update(UpdateAdministrationRolesCommand command, CancellationToken ct)
    {
        var validation = Validate(command);
        if (validation.IsError)
            return validation;

        // Everything that can fail is resolved before anything is changed
        var roleChangesResult = await GetRoleChanges(command, ct);
        if (roleChangesResult.IsError)
            return roleChangesResult.Error;

        return await handleRoleChangesTask.ExecuteRoleChanges(roleChangesResult.Value, ct);
    }

    /// <summary>
    /// Diffs the requested roles with the current ones in Keycloak and resolves the user and group ids needed to apply
    /// them
    /// </summary>
    private async Task<Result<RoleChange<AdministrationRole>[]>> GetRoleChanges(UpdateAdministrationRolesCommand command,
        CancellationToken ct)
    {
        var currentResult = await getAdministrationUsersTask.GetUsers(ct);
        if (currentResult.IsError)
        {
            logger.LogError("Loading the current administration roles failed: {Error}", currentResult.Error);
            return new LoadCurrentAdministrationRolesError(currentResult.Error);
        }

        // Only this user is requested, so the other users must not be in the diff or they would lose all their roles
        return await handleRoleChangesTask.GetRoleChanges(
            currentResult.Value.AdministrationUsers
                .Where(x => x.Username.Equals(command.Username, StringComparison.OrdinalIgnoreCase))
                .Select(x => new UserWithRoles<AdministrationRole>(x.Username, x.Roles)),
            [new UserWithRoles<AdministrationRole>(command.Username, command.AdministrationRoles)],
            KeycloakGroups.GetAdministrationRoleGroup,
            ct
        );
    }

    /// <summary>
    /// The JSON deserialization does not enforce non-nullable references and accepts undefined enum values, so they are
    /// checked here
    /// </summary>
    private static Result Validate(UpdateAdministrationRolesCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Username))
            return new InvalidInputError(nameof(UpdateAdministrationRolesCommand.Username), "Username must not be empty");

        var undefinedRole = command.AdministrationRoles.Where(x => !Enum.IsDefined(x)).Cast<AdministrationRole?>()
            .FirstOrDefault();
        if (undefinedRole is not null)
            return new InvalidInputError(nameof(UpdateAdministrationRolesCommand.AdministrationRoles), $"Role '{undefinedRole}' does not exist");

        return Result.Ok();
    }
}
