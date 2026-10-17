namespace eFinanzer.Common.Auth;

public class AccessForbiddenException(AccessForbiddenException.RequiredGroupsType type, string[] groupPaths) 
    : Exception($"User must be in {(type is RequiredGroupsType.MustBeInAllGroups ? "all" : "one")} of the following groups: {string.Join(", ", groupPaths)}")
{
    public static AccessForbiddenException MustBeInAllGroups(params string[] groupPaths)
        => new(RequiredGroupsType.MustBeInAllGroups, groupPaths);
    
    public static AccessForbiddenException MustBeInAnyGroups(params string[] groupPaths)
        => new(RequiredGroupsType.MustBeInAnyGroup, groupPaths);

    public enum RequiredGroupsType
    {
        MustBeInAllGroups,
        MustBeInAnyGroup
    }
}