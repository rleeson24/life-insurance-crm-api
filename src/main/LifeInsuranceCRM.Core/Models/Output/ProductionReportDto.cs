using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Output;

[ExcludeFromCodeCoverage]

public sealed class ProductionReportDto
{
    public short PlanYear { get; init; }

    public IReadOnlyList<ProductionReportRowDto> Medicare { get; init; } = [];

    public IReadOnlyList<ProductionReportRowDto> Drug { get; init; } = [];

    public IReadOnlyList<ProductionReportRowDto> Secondary { get; init; } = [];
}
