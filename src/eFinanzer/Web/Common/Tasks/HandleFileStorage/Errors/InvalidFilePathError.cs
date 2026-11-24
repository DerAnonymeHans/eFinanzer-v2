namespace eFinanzer.Common.Tasks.HandleFileStorage.Errors;

/// <summary>
/// The path is empty, absolute or points outside of the storage directory
/// </summary>
public record InvalidFilePathError(string Path) : Error($"The file path '{Path}' is not inside the file storage");
