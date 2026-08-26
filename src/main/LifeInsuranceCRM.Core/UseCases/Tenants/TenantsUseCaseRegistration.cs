using Microsoft.Extensions.DependencyInjection;

namespace LifeInsuranceCRM.Core.UseCases.Tenants;

public static class TenantsUseCaseRegistration
{
    public static IServiceCollection AddTenantsUseCases(this IServiceCollection services)
    {
        services.AddScoped<IListTenantsUseCase, ListTenantsUseCase>();
        services.AddScoped<ICreateTenantUseCase, CreateTenantUseCase>();
        services.AddScoped<IUpdateTenantUseCase, UpdateTenantUseCase>();
        return services;
    }
}
