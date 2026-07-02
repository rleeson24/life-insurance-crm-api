using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LifeInsuranceCRM.Core;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSharedCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<INowProvider, UtcNowProvider>();
        services.AddScoped<IProcessRequestFactory, ProcessRequestFactory>();
        services.AddScoped<IAuthSecurityEventRecorder, AuthSecurityEventRecorder>();
        return services;
    }

    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSharedCoreServices();
        return services;
    }
}
