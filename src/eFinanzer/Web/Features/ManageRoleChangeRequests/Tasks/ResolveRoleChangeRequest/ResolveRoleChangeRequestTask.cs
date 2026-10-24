using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Integrations.Keycloak.Clients.UserClient;
using eFinanzer.Common.Integrations.Keycloak.Constants;
using eFinanzer.Common.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Common.Tasks.GetRoleChanges;
using eFinanzer.Common.Tasks.GetRoleChanges.Models;
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
    IHandleRoleChangesTask handleRoleChangesTask,
    TimeProvider timeProvider,
    ILogger<ResolveRoleChangeRequestTask> logger
    ) : IResolveRoleChangeRequestTask
{
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
        RoleChange<AccountingUnitRole>[] roleChanges = [];
        if (command.Resolution == RoleChangeRequest.ResolutionStatus.Approved)
        {
            var userResult = await keycloakUserClient.GetUserByUsername(request.RequestedBy, ct);
            if (userResult.IsError)
                return new RoleChangeRequestNotApplicableError(command.Id, $"user '{request.RequestedBy}' does not exist anymore", userResult.Error);

            var roleChangesResult = await GetRoleChanges(command.Id, request, userResult.Value.Id, ct);
            if (roleChangesResult.IsError)
                return roleChangesResult.Error;
            roleChanges = roleChangesResult.Value;
        }

        await context.RoleChangeRequests
            .Where(x => x.Id == command.Id && x.Resolution == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Resolution, command.Resolution)
                .SetProperty(x => x.ResolvedBy, command.ResolvedBy)
                .SetProperty(x => x.ResolvedAt, timeProvider.GetUtcNow())
                .SetProperty(x => x.ResolutionComment, command.Comment), ct);

        var executeResult = await handleRoleChangesTask.ExecuteRoleChanges(roleChanges, ct);
        if (executeResult.IsError)
        {
            logger.LogError("Applying role change request {RoleChangeRequestId} of user {UserName} failed, reverting its resolution",
                command.Id, request.RequestedBy);

            // Not cancellable, so the request does not stay approved without all its changes being applied
            await RevertResolution(command.Id);

            return new ApplyRoleChangeError(command.Id, executeResult.Error);
        }

        logger.LogInformation("Resolved role change request {RoleChangeRequestId} of user {UserName} as {Resolution}",
            command.Id, request.RequestedBy, command.Resolution);
        return Result.Ok();
    }

    /// <summary>
    /// Resolves the Keycloak group ids of the requested roles
    /// </summary>
    private async Task<Result<RoleChange<AccountingUnitRole>[]>> GetRoleChanges(int id, RoleChangeRequest request,
        string userId, CancellationToken ct)
    {
        var roleChanges = new List<RoleChange<AccountingUnitRole>>();
        foreach (var change in request.RoleChanges)
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

            roleChanges.Add(new RoleChange<AccountingUnitRole>
            {
                Username = request.RequestedBy,
                UserId = userId,
                Role = change.AccountingUnitRole,
                KeycloakGroupId = groupIdResult.Value,
                Type = change.IsAdded ? RoleChangeType.Added : RoleChangeType.Removed,
            });
        }

        // Removals first, so a failure leaves the user with fewer instead of more rights
        return roleChanges.OrderBy(x => x.Type is RoleChangeType.Added).ToArray();
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
