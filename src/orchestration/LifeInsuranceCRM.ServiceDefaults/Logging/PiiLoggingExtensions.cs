using LifeInsuranceCRM.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LifeInsuranceCRM.ServiceDefaults;

internal static class PiiLoggingExtensions
{
    public static IServiceCollection AddPiiSanitizingLoggerFactory(this IServiceCollection services)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType != typeof(ILoggerFactory))
            {
                continue;
            }

            var registration = services[i];
            services.RemoveAt(i);
            services.Insert(
                i,
                new ServiceDescriptor(
                    typeof(ILoggerFactory),
                    sp => new PiiSanitizingLoggerFactory(CreateInnerFactory(sp, registration)),
                    registration.Lifetime));
            return services;
        }

        return services;
    }

    private static ILoggerFactory CreateInnerFactory(IServiceProvider sp, ServiceDescriptor registration)
    {
        if (registration.ImplementationInstance is ILoggerFactory instance)
        {
            return instance;
        }

        if (registration.ImplementationFactory is { } factory)
        {
            return (ILoggerFactory)factory(sp);
        }

        return (ILoggerFactory)ActivatorUtilities.CreateInstance(sp, registration.ImplementationType!);
    }
}
