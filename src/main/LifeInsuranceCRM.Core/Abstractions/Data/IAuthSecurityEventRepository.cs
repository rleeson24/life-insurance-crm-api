using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface IAuthSecurityEventRepository
{
    Task RecordAsync(AuthSecurityEvent securityEvent, CancellationToken cancellationToken = default);

    Task<ListAuthSecurityEventsResult> ListAsync(
        ListAuthSecurityEventsRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthSecurityEventDto?> GetByIdAsync(
        Guid authSecurityEventId,
        CancellationToken cancellationToken = default);
}
