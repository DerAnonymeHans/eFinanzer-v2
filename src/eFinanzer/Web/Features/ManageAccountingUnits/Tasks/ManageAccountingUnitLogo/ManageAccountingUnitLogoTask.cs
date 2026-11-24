using System.Net.Mime;
using eFinanzer.Common.Database;
using eFinanzer.Common.Database.Models;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Common.Tasks.HandleFileStorage;
using eFinanzer.Features.ManageAccountingUnits.Tasks.ManageAccountingUnitLogo.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.ManageAccountingUnitLogo.Models;
using Microsoft.EntityFrameworkCore;

namespace eFinanzer.Features.ManageAccountingUnits.Tasks.ManageAccountingUnitLogo;

public interface IManageAccountingUnitLogoTask
{
    /// <summary>
    /// Checks that the file has a PNG, JPG or SVG extension and is at most
    /// <see cref="ManageAccountingUnitLogoTask.MaxSizeInBytes"/> large
    /// </summary>
    Result Validate(IFormFile logo);

    /// <summary>
    /// Stores the logo of the accounting unit, replacing its previous one. Does not check that the unit exists, so it
    /// can be called before the creation of the unit is committed.
    /// </summary>
    Task<Result> Save(string key, IFormFile logo, CancellationToken ct);

    /// <summary>
    /// Fails with a <see cref="NotFoundError"/> if the accounting unit does not exist or has no logo
    /// </summary>
    Task<Result<AccountingUnitLogo>> Load(string key, CancellationToken ct);

    /// <summary>
    /// Deletes the logo of the accounting unit. Succeeds if it has none.
    /// </summary>
    Result Delete(string key);
}

/// <summary>
/// Every accounting unit has at most one logo. It is stored as <c>logo</c> with the lower case extension of the
/// uploaded file. Saving a logo deletes the ones with other extensions, so there is never more than one.
/// </summary>
public class ManageAccountingUnitLogoTask(
    EFinanzerDbContext context,
    IHandleFileStorageTask fileStorageTask,
    ILogger<ManageAccountingUnitLogoTask> logger
    ) : IManageAccountingUnitLogoTask
{
    public const long MaxSizeInBytes = 5 * 1024 * 1024;
    public const string LogoField = "Logo";
    public const string LogoEntity = "AccountingUnitLogo";

    /// <summary>
    /// Allowed extensions and the content type a logo with them is served with
    /// </summary>
    private static readonly Dictionary<string, string> ContentTypesByExtension = new()
    {
        [".png"] = MediaTypeNames.Image.Png,
        [".jpg"] = MediaTypeNames.Image.Jpeg,
        [".jpeg"] = MediaTypeNames.Image.Jpeg,
        // SVGs can contain scripts, the load endpoint serves logos with a content security policy that blocks them
        [".svg"] = MediaTypeNames.Image.Svg,
    };

    public Result Validate(IFormFile logo)
    {
        if (GetExtension(logo) is null)
            return new InvalidInputError(LogoField, "Logo must be a PNG, JPG or SVG file (.png, .jpg, .jpeg or .svg)");

        if (logo.Length == 0)
            return new InvalidInputError(LogoField, "Logo must not be empty");

        if (logo.Length > MaxSizeInBytes)
            return new InvalidInputError(LogoField, $"Logo must not be larger than {MaxSizeInBytes / 1024 / 1024} MB");

        return Result.Ok();
    }

    public async Task<Result> Save(string key, IFormFile logo, CancellationToken ct)
    {
        var validationResult = Validate(logo);
        if (validationResult.IsError)
            return validationResult;

        var extension = GetExtension(logo)!;

        await using (var content = logo.OpenReadStream())
        {
            var saveResult = await fileStorageTask.Save(GetLogoPath(key, extension), content, ct);
            if (saveResult.IsError)
            {
                logger.LogError("Saving the logo of accounting unit {AccountingUnitKey} failed: {Error}", key, saveResult.Error);
                return new SaveAccountingUnitLogoError(key, saveResult.Error);
            }
        }

        // Deleted after the new logo is stored, so a failed save keeps the previous logo
        foreach (var otherExtension in ContentTypesByExtension.Keys.Where(x => x != extension))
        {
            var deleteResult = fileStorageTask.Delete(GetLogoPath(key, otherExtension));
            if (deleteResult.IsError)
            {
                logger.LogError("Deleting the previous logo of accounting unit {AccountingUnitKey} failed: {Error}",
                    key, deleteResult.Error);
                return new SaveAccountingUnitLogoError(key, deleteResult.Error);
            }
        }

        logger.LogInformation("Saved the logo of accounting unit {AccountingUnitKey}", key);
        return Result.Ok();
    }

    public async Task<Result<AccountingUnitLogo>> Load(string key, CancellationToken ct)
    {
        var unitExists = await context.AccountingUnits.AnyAsync(x => x.Key == key, ct);
        if (!unitExists)
            return new NotFoundError(nameof(AccountingUnit), key);

        foreach (var (extension, contentType) in ContentTypesByExtension)
        {
            var loadResult = await fileStorageTask.Load(GetLogoPath(key, extension), ct);
            if (loadResult.IsSuccess)
                return new AccountingUnitLogo(loadResult.Value, contentType);

            if (loadResult.Error is not NotFoundError)
            {
                logger.LogError("Loading the logo of accounting unit {AccountingUnitKey} failed: {Error}", key, loadResult.Error);
                return new LoadAccountingUnitLogoError(key, loadResult.Error);
            }
        }

        return new NotFoundError(LogoEntity, key);
    }

    public Result Delete(string key)
    {
        foreach (var extension in ContentTypesByExtension.Keys)
        {
            var deleteResult = fileStorageTask.Delete(GetLogoPath(key, extension));
            if (deleteResult.IsError)
            {
                logger.LogError("Deleting the logo of accounting unit {AccountingUnitKey} failed: {Error}", key, deleteResult.Error);
                return new DeleteAccountingUnitLogoError(key, deleteResult.Error);
            }
        }

        return Result.Ok();
    }

    /// <summary>
    /// Lower case extension of the uploaded file, or null if it is not allowed
    /// </summary>
    private static string? GetExtension(IFormFile logo)
    {
        var extension = Path.GetExtension(logo.FileName).ToLowerInvariant();
        return ContentTypesByExtension.ContainsKey(extension) ? extension : null;
    }

    /// <summary>
    /// The key is validated by <see cref="AccountingUnitValidation.ValidateKey"/>, so it is safe to use in a path
    /// </summary>
    private static string GetLogoPath(string key, string extension)
        => Path.Combine("accounting-units", key, $"logo{extension}");
}
