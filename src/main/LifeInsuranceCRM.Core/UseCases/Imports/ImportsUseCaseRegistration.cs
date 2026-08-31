using Microsoft.Extensions.DependencyInjection;

namespace LifeInsuranceCRM.Core.UseCases.Imports;

public static class ImportsUseCaseRegistration
{
    public static IServiceCollection AddImportsUseCases(this IServiceCollection services)
    {
        services.AddScoped<IImportAccessDatabaseUseCase, ImportAccessDatabaseUseCase>();
        return services;
    }
}
