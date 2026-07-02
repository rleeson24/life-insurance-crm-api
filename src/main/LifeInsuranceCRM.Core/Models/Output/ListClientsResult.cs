namespace LifeInsuranceCRM.Core.Models.Output;

public sealed class ListClientsResult
{
    public IReadOnlyList<ClientSummaryDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
