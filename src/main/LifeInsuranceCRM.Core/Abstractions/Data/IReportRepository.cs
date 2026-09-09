using LifeInsuranceCRM.Core.Models.Output;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface IReportRepository
{
    Task<BookOfBusinessReportDto> GetBookOfBusinessAsync(CancellationToken cancellationToken = default);

    Task<MailingListReportDto> GetMailingListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionReportRowDto>> ListMedicareProductionAsync(
        short planYear,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionReportRowDto>> ListDrugProductionAsync(
        short planYear,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionReportRowDto>> ListSecondaryProductionAsync(
        short planYear,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RetentionReportRowDto>> ListRetentionAsync(CancellationToken cancellationToken = default);
}
