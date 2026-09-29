using Microsoft.Extensions.DependencyInjection;

namespace LifeInsuranceCRM.Core.UseCases.AuthSecurityEvents;

public static class AuthSecurityEventsUseCaseRegistration
{
    public static IServiceCollection AddAuthSecurityEventsUseCases(this IServiceCollection services)
    {
        services.AddScoped<IListAuthSecurityEventsUseCase, ListAuthSecurityEventsUseCase>();
        services.AddScoped<IGetAuthSecurityEventUseCase, GetAuthSecurityEventUseCase>();
        services.AddScoped<IRecordAuthSessionEventUseCase, RecordAuthSessionEventUseCase>();
        return services;
    }
}
