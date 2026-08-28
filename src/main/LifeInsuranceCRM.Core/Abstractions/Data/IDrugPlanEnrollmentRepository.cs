using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface IDrugPlanEnrollmentRepository
{
    Task<IReadOnlyList<DrugPlanEnrollment>> ListByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default);

    Task<DrugPlanEnrollment?> GetByIdAsync(Guid clientId, Guid drugPlanEnrollmentId, CancellationToken cancellationToken = default);

    Task<DrugPlanEnrollment> InsertAsync(CreateDrugPlanEnrollmentModel model, Guid tenantId, AuditStamp audit, CancellationToken cancellationToken = default);

    Task<DrugPlanEnrollment?> UpdateAsync(UpdateDrugPlanEnrollmentModel model, AuditStamp audit, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(Guid clientId, Guid drugPlanEnrollmentId, AuditStamp audit, CancellationToken cancellationToken = default);
}
