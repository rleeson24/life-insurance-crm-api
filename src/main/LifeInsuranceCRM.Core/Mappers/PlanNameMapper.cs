using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models.Output;

namespace LifeInsuranceCRM.Core.Mappers;

public interface IPlanNameMapper
{
    PlanNameDto ToDto(PlanName planName);
}

public sealed class PlanNameMapper : IPlanNameMapper
{
    public PlanNameDto ToDto(PlanName planName) => new()
    {
        PlanNameId = planName.PlanNameId,
        PlanYear = planName.PlanYear,
        Name = planName.Name,
        CreatedAt = planName.CreatedAt,
        UpdatedAt = planName.UpdatedAt,
    };
}
