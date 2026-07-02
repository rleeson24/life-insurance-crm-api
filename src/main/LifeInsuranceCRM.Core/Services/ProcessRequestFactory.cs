using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Services;

public sealed class ProcessRequestFactory : Abstractions.Services.IProcessRequestFactory
{
    public ProcessRequest<TPayload> Create<TPayload>(TPayload payload, CancellationToken cancellationToken = default) =>
        ProcessRequest<TPayload>.From(payload, cancellationToken);
}
