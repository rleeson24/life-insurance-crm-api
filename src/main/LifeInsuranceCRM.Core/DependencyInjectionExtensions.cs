using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Services;
using LifeInsuranceCRM.Core.UseCases.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace LifeInsuranceCRM.Core;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSharedCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<INowProvider, UtcNowProvider>();
        services.AddScoped<IProcessRequestFactory, ProcessRequestFactory>();
        services.AddScoped<IAuthSecurityEventRecorder, AuthSecurityEventRecorder>();
        services.AddScoped<IClientMapper, ClientMapper>();
        services.AddScoped<IClientUseCaseHelpers, ClientUseCaseHelpers>();
        return services;
    }

    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSharedCoreServices();
        services.AddClientsUseCases();
        return services;
    }
}
