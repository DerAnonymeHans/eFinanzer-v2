namespace eFinanzer.Common.Auth;

public class AccessForbiddenException(string[] groupPaths) 
    : Exception($"User must be in one of the following groups: {string.Join(", ", groupPaths)}");