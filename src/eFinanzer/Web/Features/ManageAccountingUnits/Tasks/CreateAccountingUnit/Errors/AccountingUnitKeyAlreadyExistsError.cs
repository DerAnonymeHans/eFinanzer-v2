namespace eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit.Errors;

public record AccountingUnitKeyAlreadyExistsError(string Key)
    : Error($"An accounting unit with the key '{Key}' already exists");
