namespace eFinanzer.Common.Tasks.HandleFileStorage.Errors;

/// <summary>
/// Reading or writing the file failed, e.g. because of missing permissions or a full disk
/// </summary>
public record FileStorageError(string Path, string Reason) : Error($"Could not access the file '{Path}': {Reason}");
