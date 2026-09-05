using LifeInsuranceCRM.Core.Abstractions.Security;
using LifeInsuranceCRM.Core.Config;
using LifeInsuranceCRM.Providers.Security;
using Microsoft.Extensions.DependencyInjection;

namespace LifeInsuranceCRM.Providers;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddFieldEncryption(this IServiceCollection services)
    {
        services.AddSingleton<IFieldEncryptionService, KeyVaultFieldEncryptionService>();
        return services;
    }
}
