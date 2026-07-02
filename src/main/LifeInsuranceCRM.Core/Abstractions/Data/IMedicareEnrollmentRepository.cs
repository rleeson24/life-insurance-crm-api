using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface IMedicareEnrollmentRepository
{
    Task<IReadOnlyList<MedicareEnrollment>> ListByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default);

    Task<MedicareEnrollment?> GetByIdAsync(Guid clientId, Guid medicareEnrollmentId, CancellationToken cancellationToken = default);

    Task<MedicareEnrollment> InsertAsync(CreateMedicareEnrollmentModel model, Guid tenantId, AuditStamp audit, CancellationToken cancellationToken = default);

    Task<MedicareEnrollment?> UpdateAsync(UpdateMedicareEnrollmentModel model, AuditStamp audit, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(Guid clientId, Guid medicareEnrollmentId, AuditStamp audit, CancellationToken cancellationToken = default);
}
