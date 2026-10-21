using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Errors;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageRoleChangeRequests.Tasks.DeleteRoleChangeRequest;

public interface IDeleteRoleChangeRequestTask
{
    /// <summary>
    /// Deletes an unresolved request of the user. Requests of other users are reported as not found, so their
    /// existence is not revealed.
    /// </summary>
    Task<Result> Delete(int id, string userName, CancellationToken ct);
}

public class DeleteRoleChangeRequestTask(
    EFinanzerDbContext context,
    ILogger<DeleteRoleChangeRequestTask> logger
    ) : IDeleteRoleChangeRequestTask
{
    public async Task<Result> Delete(int id, string userName, CancellationToken ct)
    {
        // Checked and deleted in one statement, so the request cannot be resolved in between
        var deletedRows = await context.RoleChangeRequests
            .Where(x => x.Id == id && x.RequestedBy == userName && x.Resolution == null)
            .ExecuteDeleteAsync(ct);

        if (deletedRows == 0)
        {
            var isResolved = await context.RoleChangeRequests
                .Where(x => x.Id == id && x.RequestedBy == userName)
                .Select(x => (bool?)(x.Resolution != null))
                .FirstOrDefaultAsync(ct);

            return isResolved is true
                ? new RoleChangeRequestAlreadyResolvedError(id)
                : new NotFoundError(nameof(RoleChangeRequest), id.ToString());
        }

        logger.LogInformation("Deleted role change request {RoleChangeRequestId}", id);
        return Result.Ok();
    }
}
