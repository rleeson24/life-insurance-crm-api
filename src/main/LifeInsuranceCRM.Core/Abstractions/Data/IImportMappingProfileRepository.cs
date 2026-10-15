using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface IImportMappingProfileRepository
{
    Task<IReadOnlyList<ImportMappingProfile>> ListAsync(CancellationToken cancellationToken = default);

    Task<ImportMappingProfile?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<ImportMappingProfile> InsertAsync(
        Guid tenantId,
        string name,
        string definition,
        AuditStamp audit,
        CancellationToken cancellationToken = default);

    Task<ImportMappingProfile?> UpdateAsync(
        Guid importMappingProfileId,
        string definition,
        AuditStamp audit,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(
        Guid importMappingProfileId,
        AuditStamp audit,
        CancellationToken cancellationToken = default);
}
