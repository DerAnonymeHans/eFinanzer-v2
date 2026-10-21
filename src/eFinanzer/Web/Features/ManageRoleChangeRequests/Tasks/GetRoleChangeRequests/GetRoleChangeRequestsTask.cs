using eFinanzer.Common.Database;
using eFinanzer.Common.Models;
using eFinanzer.Common.Pagination;
using eFinanzer.Common.Pagination.Errors;
using eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetRoleChangeRequests.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageRoleChangeRequests.Tasks.GetRoleChangeRequests;

public interface IGetRoleChangeRequestsTask
{
    /// <summary>
    /// Unresolved requests of all users, oldest first
    /// </summary>
    Task<Result<Page<GetRoleChangeRequestDto>>> GetPending(OffsetPagination pagination, CancellationToken ct);

    /// <summary>
    /// Resolved and unresolved requests of the user, newest first
    /// </summary>
    Task<Result<GetRoleChangeRequestDto[]>> GetOwn(string userName, CancellationToken ct);
}

public class GetRoleChangeRequestsTask(
    EFinanzerDbContext context
    ) : IGetRoleChangeRequestsTask
{
    internal const int MaxPageSize = 50;
    
    public async Task<Result<Page<GetRoleChangeRequestDto>>> GetPending(OffsetPagination pagination, CancellationToken ct)
    {
        if (!pagination.IsValid(MaxPageSize))
            return new InvalidOffsetPaginationError(pagination, MaxPageSize);
        
        var requestsPage = await context.RoleChangeRequests
            .Where(x => x.Resolution == null)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToPage(pagination, ct);

        return requestsPage.Select(GetRoleChangeRequestDto.FromEntity);
    }

    public async Task<Result<GetRoleChangeRequestDto[]>> GetOwn(string userName, CancellationToken ct)
    {
        var requests = await context.RoleChangeRequests
            .Where(x => x.RequestedBy == userName)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToArrayAsync(ct);

        return requests.Select(GetRoleChangeRequestDto.FromEntity).ToArray();
    }
}
