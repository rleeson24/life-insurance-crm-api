using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Output;

[ExcludeFromCodeCoverage]

public sealed class RetentionReportRowDto
{
    public int FirstYear { get; init; }

    public int TotalCount { get; init; }

    public int StillActiveCount { get; init; }
}

[ExcludeFromCodeCoverage]

public sealed class RetentionReportDto
{
    public IReadOnlyList<RetentionReportRowDto> Rows { get; init; } = [];
}
