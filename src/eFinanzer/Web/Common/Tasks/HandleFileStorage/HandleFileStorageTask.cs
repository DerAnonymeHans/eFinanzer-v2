using eFinanzer.Common.Results.Errors;
using eFinanzer.Common.Tasks.HandleFileStorage.Errors;
using Microsoft.Extensions.Options;

namespace eFinanzer.Common.Tasks.HandleFileStorage;

public interface IHandleFileStorageTask
{
    /// <summary>
    /// Stores the content at the path relative to the storage directory. An existing file is replaced.
    /// </summary>
    Task<Result> Save(string relativePath, Stream content, CancellationToken ct);

    /// <summary>
    /// Opens the file at the path relative to the storage directory. The caller has to dispose the returned stream.
    /// Fails with a <see cref="NotFoundError"/> if there is no such file.
    /// </summary>
    Task<Result<Stream>> Load(string relativePath, CancellationToken ct);

    /// <summary>
    /// Deletes the file at the path relative to the storage directory. Succeeds if there is no such file.
    /// </summary>
    Result Delete(string relativePath);
}

/// <summary>
/// Stores files on disk below <see cref="FileStorageOptions.BasePath"/>
/// </summary>
public class HandleFileStorageTask(
    IOptions<FileStorageOptions> options,
    IHostEnvironment environment,
    ILogger<HandleFileStorageTask> logger
    ) : IHandleFileStorageTask
{
    private const string FileEntity = "File";

    private readonly string _basePath = Path.GetFullPath(options.Value.BasePath, environment.ContentRootPath);

    public async Task<Result> Save(string relativePath, Stream content, CancellationToken ct)
    {
        var pathResult = GetFullPath(relativePath);
        if (pathResult.IsError)
            return pathResult.Error;

        var fullPath = pathResult.Value;
        // Written next to the target and moved afterwards, so a failed write never leaves a half written file behind
        var tempPath = $"{fullPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            await using (var file = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                             bufferSize: 81920, useAsync: true))
            {
                await content.CopyToAsync(file, ct);
            }

            File.Move(tempPath, fullPath, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            TryDelete(tempPath);
            logger.LogError(exception, "Saving file {FilePath} failed", relativePath);
            return new FileStorageError(relativePath, exception.Message);
        }
        catch (OperationCanceledException)
        {
            TryDelete(tempPath);
            throw;
        }

        logger.LogInformation("Saved file {FilePath}", relativePath);
        return Result.Ok();
    }

    public Task<Result<Stream>> Load(string relativePath, CancellationToken ct)
    {
        var pathResult = GetFullPath(relativePath);
        if (pathResult.IsError)
            return Task.FromResult<Result<Stream>>(pathResult.Error);

        try
        {
            Stream file = new FileStream(pathResult.Value, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 81920, useAsync: true);
            return Task.FromResult<Result<Stream>>(file);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return Task.FromResult<Result<Stream>>(new NotFoundError(FileEntity, relativePath));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Loading file {FilePath} failed", relativePath);
            return Task.FromResult<Result<Stream>>(new FileStorageError(relativePath, exception.Message));
        }
    }

    public Result Delete(string relativePath)
    {
        var pathResult = GetFullPath(relativePath);
        if (pathResult.IsError)
            return pathResult.Error;

        try
        {
            if (!File.Exists(pathResult.Value))
                return Result.Ok();

            File.Delete(pathResult.Value);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Deleting file {FilePath} failed", relativePath);
            return new FileStorageError(relativePath, exception.Message);
        }

        logger.LogInformation("Deleted file {FilePath}", relativePath);
        return Result.Ok();
    }

    /// <summary>
    /// Resolves the path against the storage directory and makes sure it does not leave it, e.g. via "..".
    /// </summary>
    private Result<string> GetFullPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            return new InvalidFilePathError(relativePath);

        var fullPath = Path.GetFullPath(relativePath, _basePath);
        var baseWithSeparator = Path.TrimEndingDirectorySeparator(_basePath) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(baseWithSeparator, StringComparison.Ordinal))
            return new InvalidFilePathError(relativePath);

        return fullPath;
    }

    private void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(exception, "Could not delete temporary file {FilePath}", path);
        }
    }
}
