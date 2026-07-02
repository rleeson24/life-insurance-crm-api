using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.Models.Requests;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface IClientRepository
{
    Task<ListClientsResult> ListAsync(ListClientsRequest request, CancellationToken cancellationToken = default);

    Task<Client?> GetByIdAsync(Guid clientId, CancellationToken cancellationToken = default);

    Task<Client> InsertAsync(CreateClientModel model, Guid tenantId, AuditStamp audit, CancellationToken cancellationToken = default);

    Task<Client?> UpdateAsync(UpdateClientModel model, AuditStamp audit, CancellationToken cancellationToken = default);

    Task<Client?> UpdateStatusAsync(UpdateClientStatusModel model, AuditStamp audit, CancellationToken cancellationToken = default);
}
