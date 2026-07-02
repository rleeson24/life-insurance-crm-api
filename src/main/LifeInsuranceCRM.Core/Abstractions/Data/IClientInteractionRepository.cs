using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface IClientInteractionRepository
{
    Task<IReadOnlyList<ClientInteraction>> ListByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FollowUpInteractionDto>> ListFollowUpsAsync(CancellationToken cancellationToken = default);

    Task<ClientInteraction?> GetByIdAsync(Guid clientId, Guid clientInteractionId, CancellationToken cancellationToken = default);

    Task<ClientInteraction> InsertAsync(CreateClientInteractionModel model, Guid tenantId, AuditStamp audit, CancellationToken cancellationToken = default);

    Task<ClientInteraction?> UpdateAsync(UpdateClientInteractionModel model, AuditStamp audit, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(Guid clientId, Guid clientInteractionId, AuditStamp audit, CancellationToken cancellationToken = default);
}
