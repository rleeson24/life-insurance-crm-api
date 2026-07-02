using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Abstractions.Services;

public interface IProcessRequestFactory
{
    ProcessRequest<TPayload> Create<TPayload>(TPayload payload, CancellationToken cancellationToken = default);
}
