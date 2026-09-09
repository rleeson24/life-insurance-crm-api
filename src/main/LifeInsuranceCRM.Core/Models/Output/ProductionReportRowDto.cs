using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Output;

[ExcludeFromCodeCoverage]

public sealed class ProductionReportRowDto
{
    public string PlanName { get; init; } = string.Empty;

    public DateOnly? CoverageStartDate { get; init; }

    public int EnrollmentCount { get; init; }
}
