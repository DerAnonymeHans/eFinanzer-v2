namespace eFinanzer.Common.Tasks.HandleFileStorage;

/// <summary>
/// Configuration section <c>FileStorage</c>
/// </summary>
public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Directory all files are stored in. Relative paths are resolved against the content root of the app. In
    /// production it is a docker volume (see src/deploy/docker-compose.yml).
    /// </summary>
    public string BasePath { get; set; } = string.Empty;
}
