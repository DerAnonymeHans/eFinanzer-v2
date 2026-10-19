using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.CreateRoleChangeRequest.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.CreateRoleChangeRequest.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageRoleChangeRequests.Tasks.CreateRoleChangeRequest;

public interface ICreateRoleChangeRequestTask
{
    Task<Result> Create(CreateRoleChangeRequestRequest request, CancellationToken ct);
}

public class CreateRoleChangeRequestTask(
    EFinanzerDbContext context,
    TimeProvider timeProvider,
    ILogger<CreateRoleChangeRequestTask> logger
) : ICreateRoleChangeRequestTask
{
    public async Task<Result> Create(CreateRoleChangeRequestRequest request, CancellationToken ct)
    {
        Result[] validations =
        [
            RoleChangeRequestValidation.ValidateComment(nameof(request.Comment), request.Comment),
            ValidateRoleChanges(request.RoleChanges),
        ];
        var failedValidation = validations.FirstOrDefault(x => x.IsError);
        if (failedValidation is not null)
            return failedValidation;
        
        var pendingRequestForUserExists = await context.RoleChangeRequests
            .AnyAsync(x => x.RequestedBy == request.RequestedBy && x.Resolution == null, ct);
        if (pendingRequestForUserExists)
            return new PendingRoleChangeRequestAlreadyExistsError();

        var accountingUnitKeys = request.RoleChanges.Select(x => x.AccountingUnitKey).Distinct().ToList();
        var existingKeys = await context.AccountingUnits
            .Where(x => accountingUnitKeys.Contains(x.Key))
            .Select(x => x.Key)
            .ToListAsync(ct);
        var unknownKey = accountingUnitKeys.Except(existingKeys).FirstOrDefault();
        if (unknownKey is not null)
            return new NotFoundError(nameof(AccountingUnit), unknownKey);

        var roleChangeRequest = new RoleChangeRequest
        {
            RequestedBy = request.RequestedBy,
            CreatedAt = timeProvider.GetUtcNow(),
            RequestComment = request.Comment,
            RoleChanges = request.RoleChanges
                .Select(x => new RoleChangeRequest.RoleChange
                {
                    AccountingUnitKey = x.AccountingUnitKey,
                    AccountingUnitRole = x.AccountingUnitRole,
                    IsAdded = x.IsAdded,
                })
                .ToList(),
        };
        context.RoleChangeRequests.Add(roleChangeRequest);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created role change request {RoleChangeRequestId} for user {UserName}",
            roleChangeRequest.Id, request.RequestedBy);
        return Result.Ok();
    }

    /// <summary>
    /// The JSON deserialization does not enforce non-nullable references and accepts undefined enum values, so they are
    /// checked here
    /// </summary>
    private static Result ValidateRoleChanges(CreateRoleChangeRequestRequest.RoleChange[]? roleChanges)
    {
        const string field = nameof(CreateRoleChangeRequestRequest.RoleChanges);

        if (roleChanges is null || roleChanges.Length == 0)
            return new InvalidInputError(field, "At least one role change is required");

        if (roleChanges.Any(x => string.IsNullOrWhiteSpace(x.AccountingUnitKey)))
            return new InvalidInputError(field, "Accounting unit keys must not be empty");

        var undefinedRoles = roleChanges.Select(x => x.AccountingUnitRole).Where(x => !Enum.IsDefined(x)).ToList();
        if (undefinedRoles.Count > 0)
            return new InvalidInputError(field, $"Role '{undefinedRoles[0]}' does not exist");

        // Adding and removing the same role in one request contradicts itself
        var duplicate = roleChanges
            .GroupBy(x => (x.AccountingUnitKey, x.AccountingUnitRole))
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            return new InvalidInputError(field,
                $"Role {duplicate.Key.AccountingUnitRole} in accounting unit '{duplicate.Key.AccountingUnitKey}' is listed more than once");

        return Result.Ok();
    }
}
