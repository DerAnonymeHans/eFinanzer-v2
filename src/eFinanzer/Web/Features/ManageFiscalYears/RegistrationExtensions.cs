using eFinanzer.Features.ManageFiscalYears.Tasks.CreateFiscalYear;
using eFinanzer.Features.ManageFiscalYears.Tasks.DeleteFiscalYear;
using eFinanzer.Features.ManageFiscalYears.Tasks.GetFiscalYears;
using eFinanzer.Features.ManageFiscalYears.Tasks.UpdateFiscalYear;

namespace eFinanzer.Features.ManageFiscalYears;

public static class RegistrationExtensions
{
    public static IServiceCollection AddManageFiscalYears(this IServiceCollection services)
    {
        services.AddScoped<ICreateFiscalYearTask, CreateFiscalYearTask>();
        services.AddScoped<IGetFiscalYearTask, GetFiscalYearsTask>();
        services.AddScoped<IUpdateFiscalYearTask, UpdateFiscalYearTask>();
        services.AddScoped<IDeleteFiscalYearTask, DeleteFiscalYearTask>();

        return services;
    }
}
