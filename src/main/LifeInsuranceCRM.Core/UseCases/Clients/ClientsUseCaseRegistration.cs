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
        services.AddScoped<IDeleteClientUseCase, DeleteClientUseCase>();
        services.AddScoped<IListClientInteractionsUseCase, ListClientInteractionsUseCase>();
        services.AddScoped<ICreateClientInteractionUseCase, CreateClientInteractionUseCase>();
        services.AddScoped<IUpdateClientInteractionUseCase, UpdateClientInteractionUseCase>();
        services.AddScoped<IDeleteClientInteractionUseCase, DeleteClientInteractionUseCase>();
        services.AddScoped<IListMajorMedicalEnrollmentsUseCase, ListMajorMedicalEnrollmentsUseCase>();
        services.AddScoped<ICreateMajorMedicalEnrollmentUseCase, CreateMajorMedicalEnrollmentUseCase>();
        services.AddScoped<IUpdateMajorMedicalEnrollmentUseCase, UpdateMajorMedicalEnrollmentUseCase>();
        services.AddScoped<IDeleteMajorMedicalEnrollmentUseCase, DeleteMajorMedicalEnrollmentUseCase>();
        services.AddScoped<IListDrugPlanEnrollmentsUseCase, ListDrugPlanEnrollmentsUseCase>();
        services.AddScoped<ICreateDrugPlanEnrollmentUseCase, CreateDrugPlanEnrollmentUseCase>();
        services.AddScoped<IUpdateDrugPlanEnrollmentUseCase, UpdateDrugPlanEnrollmentUseCase>();
        services.AddScoped<IDeleteDrugPlanEnrollmentUseCase, DeleteDrugPlanEnrollmentUseCase>();
        services.AddScoped<IListSecondaryEnrollmentsUseCase, ListSecondaryEnrollmentsUseCase>();
        services.AddScoped<ICreateSecondaryEnrollmentUseCase, CreateSecondaryEnrollmentUseCase>();
        services.AddScoped<IUpdateSecondaryEnrollmentUseCase, UpdateSecondaryEnrollmentUseCase>();
        services.AddScoped<IDeleteSecondaryEnrollmentUseCase, DeleteSecondaryEnrollmentUseCase>();
        services.AddScoped<IListFollowUpInteractionsUseCase, ListFollowUpInteractionsUseCase>();
        return services;
    }
}
