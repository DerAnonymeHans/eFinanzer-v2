namespace eFinanzer.Common.Tasks.HandleFileStorage;

public static class RegistrationExtensions
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services)
    {
        services.AddOptions<FileStorageOptions>()
            .BindConfiguration(FileStorageOptions.SectionName)
            .Validate(x => !string.IsNullOrWhiteSpace(x.BasePath), $"'{FileStorageOptions.SectionName}:BasePath' is not configured.")
            .ValidateOnStart();

        services.AddScoped<IHandleFileStorageTask, HandleFileStorageTask>();

        return services;
    }
}
