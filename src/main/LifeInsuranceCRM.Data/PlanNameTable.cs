using LifeInsuranceCRM.Core.Constants;

namespace LifeInsuranceCRM.Data;

internal readonly record struct PlanNameTable(string TableName, string IdColumn)
{
    public static PlanNameTable For(PlanNameKind kind) => kind switch
    {
        PlanNameKind.Medicare => new("dbo.MedicarePlanNames", "MedicarePlanNameId"),
        PlanNameKind.Drug => new("dbo.DrugPlanNames", "DrugPlanNameId"),
        PlanNameKind.Secondary => new("dbo.SecondaryPlanNames", "SecondaryPlanNameId"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown plan name kind."),
    };
}
