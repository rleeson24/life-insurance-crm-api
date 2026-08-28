using Microsoft.Extensions.DependencyInjection;

namespace LifeInsuranceCRM.Core.UseCases.PlanNames;

public static class PlanNamesUseCaseRegistration
{
    public static IServiceCollection AddPlanNamesUseCases(this IServiceCollection services)
    {
        services.AddScoped<IListPlanNamesUseCase, ListPlanNamesUseCase>();
        services.AddScoped<ILookupPlanNamesUseCase, LookupPlanNamesUseCase>();
        services.AddScoped<ICreatePlanNameUseCase, CreatePlanNameUseCase>();
        services.AddScoped<IUpdatePlanNameUseCase, UpdatePlanNameUseCase>();
        services.AddScoped<IDeletePlanNameUseCase, DeletePlanNameUseCase>();
        services.AddScoped<IClonePlanNamesUseCase, ClonePlanNamesUseCase>();
        return services;
    }
}
