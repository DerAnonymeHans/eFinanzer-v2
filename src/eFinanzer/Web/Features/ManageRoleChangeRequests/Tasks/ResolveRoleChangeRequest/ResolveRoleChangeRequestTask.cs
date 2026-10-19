using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.ResolveRoleChangeRequest.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.ResolveRoleChangeRequest.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageRoleChangeRequests.Tasks.ResolveRoleChangeRequest;

public interface IResolveRoleChangeRequestTask
{
    /// <summary>
    /// Dismisses the request, or approves it and applies its role changes in Keycloak
    /// </summary>
    Task<Result> Resolve(ResolveRoleChangeRequestCommand command, CancellationToken ct);
}

public class ResolveRoleChangeRequestTask(
    EFinanzerDbContext context,
    IKeycloakUserClient keycloakUserClient,
    TimeProvider timeProvider,
    ILogger<ResolveRoleChangeRequestTask> logger
    ) : IResolveRoleChangeRequestTask
{
    private record GroupChange(string AccountingUnitKey, AccountingUnitRole Role, string GroupId, bool IsAdded);

    public async Task<Result> Resolve(ResolveRoleChangeRequestCommand command, CancellationToken ct)
    {
        Result[] validations =
        [
            RoleChangeRequestValidation.ValidateComment(nameof(command.Comment), command.Comment),
            ValidateResolution(command.Resolution),
        ];
        var failedValidation = validations.FirstOrDefault(x => x.IsError);
        if (failedValidation is not null)
            return failedValidation;

        var request = await context.RoleChangeRequests
            .FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (request is null)
            return new NotFoundError(nameof(RoleChangeRequest), command.Id.ToString());
        if (request.Resolution is not null)
            return new RoleChangeRequestAlreadyResolvedError(command.Id);

        // Everything that can fail is resolved before anything is changed
        var userId = "";
        List<GroupChange> groupChanges = [];
        if (command.Resolution == RoleChangeRequest.ResolutionStatus.Approved)
        {
            var userResult = await keycloakUserClient.GetUserByUsername(request.RequestedBy, ct);
            if (userResult.IsError)
                return new RoleChangeRequestNotApplicableError(command.Id, $"user '{request.RequestedBy}' does not exist anymore", userResult.Error);
            userId = userResult.Value.Id;

            var groupChangesResult = await GetGroupChanges(command.Id, request.RoleChanges, ct);
            if (groupChangesResult.IsError)
                return groupChangesResult.Error;
            groupChanges = groupChangesResult.Value;
        }

        await context.RoleChangeRequests
            .Where(x => x.Id == command.Id && x.Resolution == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Resolution, command.Resolution)
                .SetProperty(x => x.ResolvedBy, command.ResolvedBy)
                .SetProperty(x => x.ResolvedAt, timeProvider.GetUtcNow())
                .SetProperty(x => x.ResolutionComment, command.Comment), ct);

        foreach (var change in groupChanges)
        {
            var changeResult = change.IsAdded
                ? await keycloakUserClient.AddUserToGroup(userId, change.GroupId, ct)
                : await keycloakUserClient.RemoveUserFromGroup(userId, change.GroupId, ct);

            if (changeResult.IsError)
            {
                logger.LogError(
                    "Changing role {Role} (added: {Added}) of user {UserName} in accounting unit {AccountingUnitKey} for role change request {RoleChangeRequestId} failed: {Error}",
                    change.Role, change.IsAdded, request.RequestedBy, change.AccountingUnitKey, command.Id, changeResult.Error);

                // Not cancellable, so the request does not stay approved without all its changes being applied
                await RevertResolution(command.Id);

                return new ApplyRoleChangeError(command.Id, change.AccountingUnitKey, request.RequestedBy, change.Role,
                    change.IsAdded, changeResult.Error);
            }

            logger.LogInformation(
                "Changed role {Role} (added: {Added}) of user {UserName} in accounting unit {AccountingUnitKey} for role change request {RoleChangeRequestId}",
                change.Role, change.IsAdded, request.RequestedBy, change.AccountingUnitKey, command.Id);
        }

        logger.LogInformation("Resolved role change request {RoleChangeRequestId} of user {UserName} as {Resolution}",
            command.Id, request.RequestedBy, command.Resolution);
        return Result.Ok();
    }

    /// <summary>
    /// Resolves the Keycloak group ids of the requested roles
    /// </summary>
    private async Task<Result<List<GroupChange>>> GetGroupChanges(int id, List<RoleChangeRequest.RoleChange> roleChanges,
        CancellationToken ct)
    {
        var groupChanges = new List<GroupChange>();
        foreach (var change in roleChanges)
        {
            var groupIdResult = await keycloakUserClient.GetGroupIdFromPath(
                KeycloakGroups.GetAccountingUnitRoleGroup(change.AccountingUnitKey, change.AccountingUnitRole), ct);
            // The accounting unit may have been deleted since the request was created
            if (groupIdResult.IsError)
            {
                logger.LogWarning("Loading the group of role {Role} of accounting unit {AccountingUnitKey} failed: {Error}",
                    change.AccountingUnitRole, change.AccountingUnitKey, groupIdResult.Error);
                return new RoleChangeRequestNotApplicableError(id,
                    $"the group of role {change.AccountingUnitRole} in accounting unit '{change.AccountingUnitKey}' does not exist",
                    groupIdResult.Error);
            }

            groupChanges.Add(new GroupChange(change.AccountingUnitKey, change.AccountingUnitRole, groupIdResult.Value,
                change.IsAdded));
        }

        return groupChanges.OrderBy(x => x.IsAdded).ToList();
    }

    private async Task RevertResolution(int roleChangeRequestId)
    {
        await context.RoleChangeRequests
            .Where(x => x.Id == roleChangeRequestId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Resolution, (RoleChangeRequest.ResolutionStatus?)null)
                .SetProperty(x => x.ResolvedBy, (string?)null)
                .SetProperty(x => x.ResolvedAt, (DateTimeOffset?)null)
                .SetProperty(x => x.ResolutionComment, (string?)null), CancellationToken.None);
    }

    /// <summary>
    /// The JSON deserialization accepts undefined enum values, so they are checked here
    /// </summary>
    private static Result ValidateResolution(RoleChangeRequest.ResolutionStatus resolution)
    {
        if (!Enum.IsDefined(resolution))
            return new InvalidInputError(nameof(ResolveRoleChangeRequestCommand.Resolution),
                $"Resolution '{resolution}' does not exist");

        return Result.Ok();
    }
}
