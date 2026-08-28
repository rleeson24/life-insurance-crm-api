using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface ISecondaryEnrollmentRepository
{
    Task<IReadOnlyList<SecondaryEnrollment>> ListByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default);

    Task<SecondaryEnrollment?> GetByIdAsync(Guid clientId, Guid secondaryEnrollmentId, CancellationToken cancellationToken = default);

    Task<SecondaryEnrollment> InsertAsync(CreateSecondaryEnrollmentModel model, Guid tenantId, AuditStamp audit, CancellationToken cancellationToken = default);

    Task<SecondaryEnrollment?> UpdateAsync(UpdateSecondaryEnrollmentModel model, AuditStamp audit, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(Guid clientId, Guid secondaryEnrollmentId, AuditStamp audit, CancellationToken cancellationToken = default);
}
