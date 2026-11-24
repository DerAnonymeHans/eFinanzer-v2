namespace eFinanzer.Features.ManageAccountingUnits.Tasks.ManageAccountingUnitLogo.Models;

/// <param name="Content">Image data. The receiver has to dispose it.</param>
/// <param name="ContentType">Media type of the image, <c>image/png</c> or <c>image/jpeg</c></param>
public record AccountingUnitLogo(Stream Content, string ContentType);
