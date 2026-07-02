using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface ISupplementalEnrollmentRepository
{
    Task<IReadOnlyList<SupplementalEnrollment>> ListByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default);

    Task<SupplementalEnrollment?> GetByIdAsync(Guid clientId, Guid supplementalEnrollmentId, CancellationToken cancellationToken = default);

    Task<SupplementalEnrollment> InsertAsync(CreateSupplementalEnrollmentModel model, Guid tenantId, AuditStamp audit, CancellationToken cancellationToken = default);

    Task<SupplementalEnrollment?> UpdateAsync(UpdateSupplementalEnrollmentModel model, AuditStamp audit, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(Guid clientId, Guid supplementalEnrollmentId, AuditStamp audit, CancellationToken cancellationToken = default);
}
