using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace LifeInsuranceCRM.Data;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddDataServices(this IServiceCollection services)
    {
        services.AddSingleton<IDbExecutor, DbExecutor>();
        services.AddScoped<IAuthSecurityEventRepository, AuthSecurityEventRepository>();
        services.AddScoped<IOrganizationUserRepository, OrganizationUserRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IClientInteractionRepository, ClientInteractionRepository>();
        services.AddScoped<IMedicareEnrollmentRepository, MedicareEnrollmentRepository>();
        services.AddScoped<ISupplementalEnrollmentRepository, SupplementalEnrollmentRepository>();
        return services;
    }
}
