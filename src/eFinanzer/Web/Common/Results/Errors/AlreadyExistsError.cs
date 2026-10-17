using System.Net;

namespace eFinanzer.Common.Results.Errors;

public record AlreadyExistsError(string Entity, string Key) : Error($"{Entity} '{Key}' already exists", HttpStatusCode.Conflict);
