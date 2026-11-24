using System.Net.Mime;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Common.Tasks.HandleFileStorage;
using eFinanzer.Common.Tasks.HandleFileStorage.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.ManageAccountingUnitLogo;
using eFinanzer.Features.ManageAccountingUnits.Tasks.ManageAccountingUnitLogo.Errors;
using eFinanzer.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace eFinanzer.Tests.Features.ManageAccountingUnits.Tasks;

public class ManageAccountingUnitLogoTaskTests(DatabaseFixture database) : DatabaseTest(database)
{
    private readonly TemporaryDirectory _storage = new();

    private ManageAccountingUnitLogoTask MyTask => CreateTask(TestFiles.CreateFileStorageTask(_storage.Path));

    private ManageAccountingUnitLogoTask CreateTask(IHandleFileStorageTask fileStorageTask)
        => new(Database.CreateContext(), fileStorageTask, NullLogger<ManageAccountingUnitLogoTask>.Instance);

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        Context.AccountingUnits.Add(new AccountingUnit { Key = "fsr" });
        await Context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        _storage.Dispose();
        await base.DisposeAsync();
    }

    private static async Task<byte[]> ReadAll(Stream stream)
    {
        await using (stream)
        {
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, TestContext.Current.CancellationToken);
            return memory.ToArray();
        }
    }

    public static TheoryData<byte[], string, string, string> ValidLogos => new()
    {
        { TestFiles.Png, "my-logo.png", "logo.png", MediaTypeNames.Image.Png },
        { TestFiles.Jpeg, "my-logo.jpg", "logo.jpg", MediaTypeNames.Image.Jpeg },
        { TestFiles.Jpeg, "my-logo.jpeg", "logo.jpeg", MediaTypeNames.Image.Jpeg },
        { TestFiles.Svg, "my-logo.svg", "logo.svg", MediaTypeNames.Image.Svg },
        // The extension is stored in lower case
        { TestFiles.Png, "MY-LOGO.PNG", "logo.png", MediaTypeNames.Image.Png },
    };

    [Theory]
    [MemberData(nameof(ValidLogos))]
    public async Task SaveAndLoad_ValidLogo_StoresWithExtensionAndReturnsMatchingContentType(byte[] content,
        string uploadedFileName, string storedFileName, string contentType)
    {
        // Act
        var saveResult = await MyTask.Save("fsr", TestFiles.CreateFormFile(content, uploadedFileName), CancellationToken.None);
        var loadResult = await MyTask.Load("fsr", CancellationToken.None);

        // Assert
        Assert.True(saveResult.IsSuccess);
        Assert.True(loadResult.IsSuccess);
        Assert.Equal(contentType, loadResult.Value.ContentType);
        Assert.Equal(content, await ReadAll(loadResult.Value.Content));
        var storedFile = Assert.Single(Directory.GetFiles(_storage.Path, "*", SearchOption.AllDirectories));
        Assert.Equal(Path.Combine(_storage.Path, "accounting-units", "fsr", storedFileName), storedFile);
    }

    [Fact]
    public async Task Save_UnitAlreadyHasLogo_ReplacesItEvenIfTheFormatDiffers()
    {
        // Arrange
        await MyTask.Save("fsr", TestFiles.CreateFormFile(TestFiles.Png), CancellationToken.None);

        // Act
        var result = await MyTask.Save("fsr", TestFiles.CreateFormFile(TestFiles.Jpeg, "logo.jpg"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var logo = await MyTask.Load("fsr", CancellationToken.None);
        Assert.Equal(MediaTypeNames.Image.Jpeg, logo.Value!.ContentType);
        Assert.Equal(TestFiles.Jpeg, await ReadAll(logo.Value.Content));
        var storedFile = Assert.Single(Directory.GetFiles(_storage.Path, "*", SearchOption.AllDirectories));
        Assert.Equal("logo.jpg", Path.GetFileName(storedFile));
    }

    [Fact]
    public async Task Save_LogoWithMaxSize_IsStored()
    {
        // Act
        var result = await MyTask.Save("fsr",
            TestFiles.CreateFormFile(TestFiles.PngOfSize(ManageAccountingUnitLogoTask.MaxSizeInBytes)), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    public static TheoryData<byte[], string> InvalidLogos => new()
    {
        { [], "logo.png" },
        { TestFiles.Gif, "logo.gif" },
        { TestFiles.Png, "logo" },
        { TestFiles.Png, "logo.png.exe" },
        { TestFiles.PngOfSize(ManageAccountingUnitLogoTask.MaxSizeInBytes + 1), "logo.png" },
    };

    [Theory]
    [MemberData(nameof(InvalidLogos))]
    public async Task Save_InvalidLogo_ReturnsInvalidInputAndStoresNothing(byte[] content, string fileName)
    {
        // Act
        var result = await MyTask.Save("fsr", TestFiles.CreateFormFile(content, fileName), CancellationToken.None);

        // Assert
        var error = Assert.IsType<InvalidInputError>(result.Error);
        Assert.Equal(ManageAccountingUnitLogoTask.LogoField, error.Field);
        Assert.Empty(Directory.GetFiles(_storage.Path, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Save_StorageFails_ReturnsSaveError()
    {
        // Arrange
        var storageError = new FileStorageError("accounting-units/fsr/logo", "Disk full");
        var fileStorageTask = Substitute.For<IHandleFileStorageTask>();
        fileStorageTask.Save(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(storageError));

        // Act
        var result = await CreateTask(fileStorageTask).Save("fsr", TestFiles.CreateFormFile(TestFiles.Png), CancellationToken.None);

        // Assert
        Assert.Equal(new SaveAccountingUnitLogoError("fsr", storageError), result.Error);
    }

    [Fact]
    public async Task Save_DeletingPreviousLogoFails_ReturnsSaveError()
    {
        // Arrange
        var storageError = new FileStorageError("accounting-units/fsr/logo.jpg", "Permission denied");
        var fileStorageTask = Substitute.For<IHandleFileStorageTask>();
        fileStorageTask.Save(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(Result.Ok());
        fileStorageTask.Delete(Arg.Any<string>()).Returns(Result.Fail(storageError));

        // Act
        var result = await CreateTask(fileStorageTask).Save("fsr", TestFiles.CreateFormFile(TestFiles.Png), CancellationToken.None);

        // Assert
        Assert.Equal(new SaveAccountingUnitLogoError("fsr", storageError), result.Error);
    }

    [Fact]
    public async Task Load_UnknownUnit_ReturnsNotFound()
    {
        // Act
        var result = await MyTask.Load("unknown", CancellationToken.None);

        // Assert
        Assert.Equal(new NotFoundError(nameof(AccountingUnit), "unknown"), result.Error);
    }

    [Fact]
    public async Task Load_UnitWithoutLogo_ReturnsLogoNotFound()
    {
        // Act
        var result = await MyTask.Load("fsr", CancellationToken.None);

        // Assert
        Assert.Equal(new NotFoundError(ManageAccountingUnitLogoTask.LogoEntity, "fsr"), result.Error);
    }

    [Fact]
    public async Task Load_StorageFails_ReturnsLoadError()
    {
        // Arrange
        var storageError = new FileStorageError("accounting-units/fsr/logo", "Permission denied");
        var fileStorageTask = Substitute.For<IHandleFileStorageTask>();
        fileStorageTask.Load(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<Stream>.Fail(storageError));

        // Act
        var result = await CreateTask(fileStorageTask).Load("fsr", CancellationToken.None);

        // Assert
        Assert.Equal(new LoadAccountingUnitLogoError("fsr", storageError), result.Error);
    }
}
