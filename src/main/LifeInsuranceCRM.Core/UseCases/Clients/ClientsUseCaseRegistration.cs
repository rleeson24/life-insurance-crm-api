using Microsoft.Extensions.DependencyInjection;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public static class ClientsUseCaseRegistration
{
    public static IServiceCollection AddClientsUseCases(this IServiceCollection services)
    {
        services.AddScoped<IListClientsUseCase, ListClientsUseCase>();
        services.AddScoped<IGetClientUseCase, GetClientUseCase>();
        services.AddScoped<IGetClientDetailUseCase, GetClientDetailUseCase>();
        services.AddScoped<ICreateClientUseCase, CreateClientUseCase>();
        services.AddScoped<IUpdateClientUseCase, UpdateClientUseCase>();
        services.AddScoped<IUpdateClientStatusUseCase, UpdateClientStatusUseCase>();
        services.AddScoped<IListClientInteractionsUseCase, ListClientInteractionsUseCase>();
        services.AddScoped<ICreateClientInteractionUseCase, CreateClientInteractionUseCase>();
        services.AddScoped<IUpdateClientInteractionUseCase, UpdateClientInteractionUseCase>();
        services.AddScoped<IDeleteClientInteractionUseCase, DeleteClientInteractionUseCase>();
        services.AddScoped<IListMedicareEnrollmentsUseCase, ListMedicareEnrollmentsUseCase>();
        services.AddScoped<ICreateMedicareEnrollmentUseCase, CreateMedicareEnrollmentUseCase>();
        services.AddScoped<IUpdateMedicareEnrollmentUseCase, UpdateMedicareEnrollmentUseCase>();
        services.AddScoped<IDeleteMedicareEnrollmentUseCase, DeleteMedicareEnrollmentUseCase>();
        services.AddScoped<IListSupplementalEnrollmentsUseCase, ListSupplementalEnrollmentsUseCase>();
        services.AddScoped<ICreateSupplementalEnrollmentUseCase, CreateSupplementalEnrollmentUseCase>();
        services.AddScoped<IUpdateSupplementalEnrollmentUseCase, UpdateSupplementalEnrollmentUseCase>();
        services.AddScoped<IDeleteSupplementalEnrollmentUseCase, DeleteSupplementalEnrollmentUseCase>();
        services.AddScoped<IListFollowUpInteractionsUseCase, ListFollowUpInteractionsUseCase>();
        return services;
    }
}
