using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models.Output;

[ExcludeFromCodeCoverage]

public sealed class BookOfBusinessReportDto
{
    public IReadOnlyList<BookOfBusinessRowDto> Items { get; init; } = [];

    public bool Truncated { get; init; }
}
