using eFinanzer.Features.ManageAccountingUnits.Tasks.CreateAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.DeleteAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.GetAccountingUnit;
using eFinanzer.Features.ManageAccountingUnits.Tasks.UpdateAccountingUnit;

namespace eFinanzer.Features.ManageAccountingUnits;

public static class RegistrationExtensions
{
    public static IServiceCollection AddManageAccountingUnits(this IServiceCollection services)
    {
        services.AddScoped<ICreateAccountingUnitTask, CreateAccountingUnitTask>();
        services.AddScoped<IGetAccountingUnitTask, GetAccountingUnitTask>();
        services.AddScoped<IUpdateAccountingUnitTask, UpdateAccountingUnitTask>();
        services.AddScoped<IDeleteAccountingUnitTask, DeleteAccountingUnitTask>();

        return services;
    }
}
