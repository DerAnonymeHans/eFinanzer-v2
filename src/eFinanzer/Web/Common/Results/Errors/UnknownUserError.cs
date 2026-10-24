namespace eFinanzer.Common.Results.Errors;

public record UnknownUserError(string UserName) : Error($"There is no user with the username '{UserName}'");
