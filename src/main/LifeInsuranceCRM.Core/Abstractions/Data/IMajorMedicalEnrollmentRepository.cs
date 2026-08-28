using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface IMajorMedicalEnrollmentRepository
{
    Task<IReadOnlyList<MajorMedicalEnrollment>> ListByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default);

    Task<MajorMedicalEnrollment?> GetByIdAsync(Guid clientId, Guid majorMedicalEnrollmentId, CancellationToken cancellationToken = default);

    Task<MajorMedicalEnrollment> InsertAsync(CreateMajorMedicalEnrollmentModel model, Guid tenantId, AuditStamp audit, CancellationToken cancellationToken = default);

    Task<MajorMedicalEnrollment?> UpdateAsync(UpdateMajorMedicalEnrollmentModel model, AuditStamp audit, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(Guid clientId, Guid majorMedicalEnrollmentId, AuditStamp audit, CancellationToken cancellationToken = default);
}
