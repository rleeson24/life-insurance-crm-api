using Microsoft.Extensions.DependencyInjection;

namespace LifeInsuranceCRM.Core.UseCases.Imports;

public static class ImportsUseCaseRegistration
{
    public static IServiceCollection AddImportsUseCases(this IServiceCollection services)
    {
        services.AddScoped<IImportAccessDatabaseUseCase, ImportAccessDatabaseUseCase>();
        services.AddScoped<IListImportMappingProfilesUseCase, ListImportMappingProfilesUseCase>();
        services.AddScoped<ISaveImportMappingProfileUseCase, SaveImportMappingProfileUseCase>();
        services.AddScoped<IDeleteImportMappingProfileUseCase, DeleteImportMappingProfileUseCase>();
        return services;
    }
}
