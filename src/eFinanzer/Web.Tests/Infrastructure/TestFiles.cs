using eFinanzer.Common.Tasks.HandleFileStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace eFinanzer.Tests.Infrastructure;

/// <summary>
/// Minimal file contents and helpers for tests of file uploads. Only the file extension is checked, so the contents
/// do not have to be valid images.
/// </summary>
public static class TestFiles
{
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x02, 0x03];
    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x04, 0x05, 0x06];
    public static readonly byte[] Gif = "GIF89a"u8.ToArray();
    public static readonly byte[] Svg = """<svg xmlns="http://www.w3.org/2000/svg" width="1" height="1"/>"""u8.ToArray();

    public static IFormFile CreateFormFile(byte[] content, string fileName = "logo.png")
        => new FormFile(new MemoryStream(content), 0, content.Length, "Logo", fileName);

    /// <summary>
    /// File of the given size, to be uploaded as PNG
    /// </summary>
    public static byte[] PngOfSize(long size)
    {
        var content = new byte[size];
        Png.CopyTo(content, 0);
        return content;
    }

    public static HandleFileStorageTask CreateFileStorageTask(string basePath)
        => new(Options.Create(new FileStorageOptions { BasePath = basePath }), new TestHostEnvironment(),
            NullLogger<HandleFileStorageTask>.Instance);

    private class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "eFinanzer.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

/// <summary>
/// Empty directory below the temp directory, which is deleted on dispose
/// </summary>
public sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("efinanzer-tests-").FullName;

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }
}
