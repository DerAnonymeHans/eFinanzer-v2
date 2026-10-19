namespace eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit.Errors;

public record UnknownUserError(string UserName)
    : Error($"There is no user with the username '{UserName}'");
