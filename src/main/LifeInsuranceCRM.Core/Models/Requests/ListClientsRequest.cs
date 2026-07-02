namespace LifeInsuranceCRM.Core.Models.Requests;

public sealed class ListClientsRequest
{
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsAcaClient { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
