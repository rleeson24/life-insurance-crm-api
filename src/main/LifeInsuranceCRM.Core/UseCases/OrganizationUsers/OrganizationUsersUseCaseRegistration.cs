using Microsoft.Extensions.DependencyInjection;

namespace LifeInsuranceCRM.Core.UseCases.OrganizationUsers;

public static class OrganizationUsersUseCaseRegistration
{
    public static IServiceCollection AddOrganizationUsersUseCases(this IServiceCollection services)
    {
        services.AddScoped<IGetCurrentUserUseCase, GetCurrentUserUseCase>();
        services.AddScoped<IListOrganizationUsersUseCase, ListOrganizationUsersUseCase>();
        services.AddScoped<ICreateOrganizationUserUseCase, CreateOrganizationUserUseCase>();
        services.AddScoped<IUpdateOrganizationUserUseCase, UpdateOrganizationUserUseCase>();
        return services;
    }
}
