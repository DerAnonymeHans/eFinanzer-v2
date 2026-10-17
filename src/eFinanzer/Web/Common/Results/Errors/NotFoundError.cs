using System.Net;

namespace eFinanzer.Common.Results.Errors;

public record NotFoundError(string Entity, string Key) : Error($"{Entity} '{Key}' was not found", HttpStatusCode.NotFound);
