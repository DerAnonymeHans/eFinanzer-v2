namespace eFinanzer.Common.Results.Errors;

public record InvalidInputError(string Field, string Message) : Error($"Invalid input for field '{Field}': {Message}");