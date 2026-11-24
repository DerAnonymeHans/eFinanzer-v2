using eFinanzer.Common.Results.Errors;
using eFinanzer.Common.Tasks.HandleFileStorage;
using eFinanzer.Common.Tasks.HandleFileStorage.Errors;
using eFinanzer.Tests.Infrastructure;

namespace eFinanzer.Tests.Common.Tasks;

public sealed class HandleFileStorageTaskTests : IDisposable
{
    private readonly TemporaryDirectory _storage = new();

    private HandleFileStorageTask MyTask => TestFiles.CreateFileStorageTask(_storage.Path);

    public void Dispose() => _storage.Dispose();

    private static async Task<byte[]> ReadAll(Stream stream)
    {
        await using (stream)
        {
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, TestContext.Current.CancellationToken);
            return memory.ToArray();
        }
    }

    [Fact]
    public async Task Save_NewFile_CanBeLoaded()
    {
        // Arrange
        byte[] content = [1, 2, 3];

        // Act
        var saveResult = await MyTask.Save("folder/sub/file", new MemoryStream(content), CancellationToken.None);
        var loadResult = await MyTask.Load("folder/sub/file", CancellationToken.None);

        // Assert
        Assert.True(saveResult.IsSuccess);
        Assert.True(loadResult.IsSuccess);
        Assert.Equal(content, await ReadAll(loadResult.Value));
    }

    [Fact]
    public async Task Save_ExistingFile_ReplacesItWithoutLeavingTemporaryFiles()
    {
        // Arrange
        await MyTask.Save("file", new MemoryStream([1, 2, 3, 4, 5]), CancellationToken.None);

        // Act
        var saveResult = await MyTask.Save("file", new MemoryStream([9]), CancellationToken.None);

        // Assert
        Assert.True(saveResult.IsSuccess);
        var loadResult = await MyTask.Load("file", CancellationToken.None);
        Assert.Equal([9], await ReadAll(loadResult.Value!));
        Assert.Equal(["file"], Directory.GetFiles(_storage.Path).Select(Path.GetFileName));
    }

    [Fact]
    public async Task Load_MissingFile_ReturnsNotFound()
    {
        // Act
        var result = await MyTask.Load("missing/file", CancellationToken.None);

        // Assert
        Assert.Equal(new NotFoundError("File", "missing/file"), result.Error);
    }

    [Fact]
    public async Task Delete_ExistingFile_DeletesIt()
    {
        // Arrange
        await MyTask.Save("folder/file", new MemoryStream([1]), CancellationToken.None);

        // Act
        var result = MyTask.Delete("folder/file");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(new NotFoundError("File", "folder/file"), (await MyTask.Load("folder/file", CancellationToken.None)).Error);
    }

    [Fact]
    public void Delete_MissingFile_Succeeds()
    {
        // Act
        var result = MyTask.Delete("missing/file");

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("")]
    public void Delete_PathOutsideOfStorage_ReturnsInvalidFilePath(string path)
    {
        // Act
        var result = MyTask.Delete(path);

        // Assert
        Assert.Equal(new InvalidFilePathError(path), result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("../outside")]
    [InlineData("folder/../../outside")]
    [InlineData(".")]
    public async Task SaveAndLoad_PathOutsideOfStorage_ReturnsInvalidFilePath(string path)
    {
        // Act
        var saveResult = await MyTask.Save(path, new MemoryStream([1]), CancellationToken.None);
        var loadResult = await MyTask.Load(path, CancellationToken.None);

        // Assert
        Assert.Equal(new InvalidFilePathError(path), saveResult.Error);
        Assert.Equal(new InvalidFilePathError(path), loadResult.Error);
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(_storage.Path)!, "outside")));
    }

    [Fact]
    public async Task SaveAndLoad_AbsolutePath_ReturnsInvalidFilePath()
    {
        // Arrange
        var path = Path.Combine(_storage.Path, "file");

        // Act
        var saveResult = await MyTask.Save(path, new MemoryStream([1]), CancellationToken.None);
        var loadResult = await MyTask.Load(path, CancellationToken.None);

        // Assert
        Assert.Equal(new InvalidFilePathError(path), saveResult.Error);
        Assert.Equal(new InvalidFilePathError(path), loadResult.Error);
    }

    [Fact]
    public async Task Save_RelativeBasePath_IsResolvedAgainstContentRoot()
    {
        // Arrange
        var basePath = Path.GetRelativePath(AppContext.BaseDirectory, _storage.Path);
        var task = TestFiles.CreateFileStorageTask(basePath);

        // Act
        var result = await task.Save("file", new MemoryStream([1]), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(Path.Combine(_storage.Path, "file")));
    }
}
