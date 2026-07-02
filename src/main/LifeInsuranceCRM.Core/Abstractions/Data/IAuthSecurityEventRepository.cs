using LifeInsuranceCRM.Core.Entities;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface IAuthSecurityEventRepository
{
    Task RecordAsync(AuthSecurityEvent securityEvent, CancellationToken cancellationToken = default);
}
