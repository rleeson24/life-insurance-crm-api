using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Output;

[ExcludeFromCodeCoverage]
public sealed class AccessImportResultDto
{
    public int ClientsInserted { get; init; }

    public int MajorMedicalEnrollmentsInserted { get; init; }

    public int DrugPlanEnrollmentsInserted { get; init; }

    public int SecondaryEnrollmentsInserted { get; init; }

    public int InteractionsInserted { get; init; }

    public int MedicarePlanNamesInserted { get; init; }

    public int DrugPlanNamesInserted { get; init; }

    public int SecondaryPlanNamesInserted { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = [];
}
