using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace LifeInsuranceCRM.Data;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddDataServices(this IServiceCollection services)
    {
        services.AddScoped<IDbExecutor, DbExecutor>();
        services.AddScoped<IAuthSecurityEventRepository, AuthSecurityEventRepository>();
        services.AddScoped<IOrganizationUserRepository, OrganizationUserRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IClientInteractionRepository, ClientInteractionRepository>();
        services.AddScoped<IMajorMedicalEnrollmentRepository, MajorMedicalEnrollmentRepository>();
        services.AddScoped<IDrugPlanEnrollmentRepository, DrugPlanEnrollmentRepository>();
        services.AddScoped<ISecondaryEnrollmentRepository, SecondaryEnrollmentRepository>();
        services.AddScoped<IPlanNameRepository, PlanNameRepository>();
        return services;
    }
}
