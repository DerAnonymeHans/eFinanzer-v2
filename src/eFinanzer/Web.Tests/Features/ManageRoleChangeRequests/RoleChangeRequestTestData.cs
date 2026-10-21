using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Models;

namespace eFinanzer.Tests.Features.ManageRoleChangeRequests;

public static class RoleChangeRequestTestData
{
    public static RoleChangeRequest.RoleChange Change(string key, AccountingUnitRole role, bool isAdded = true)
        => new() { AccountingUnitKey = key, AccountingUnitRole = role, IsAdded = isAdded };

    public static RoleChangeRequest Pending(string requestedBy, DateTimeOffset createdAt,
        params RoleChangeRequest.RoleChange[] roleChanges) => new()
    {
        RequestedBy = requestedBy,
        CreatedAt = createdAt,
        RoleChanges = roleChanges.Length > 0 ? [.. roleChanges] : [Change("fsr", AccountingUnitRole.Treasurer)],
    };

    public static RoleChangeRequest Resolved(string requestedBy, DateTimeOffset createdAt) => Pending(requestedBy, createdAt) with
    {
        ResolvedBy = "admin",
        ResolvedAt = createdAt.AddDays(1),
        Resolution = RoleChangeRequest.ResolutionStatus.Dismissed,
    };
}
