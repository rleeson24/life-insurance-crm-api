using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Import;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface IAccessImportRepository
{
    Task<AccessImportPersistResult> ImportAsync(
        MappedAccessImport mapped,
        Guid tenantId,
        AuditStamp audit,
        CancellationToken cancellationToken = default);
}
