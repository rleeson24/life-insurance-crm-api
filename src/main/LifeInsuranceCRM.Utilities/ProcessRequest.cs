namespace LifeInsuranceCRM.Utilities;

public sealed class ProcessRequest<TPayload>
{
    private ProcessRequest(TPayload payload, CancellationToken cancellationToken)
    {
        Payload = payload;
        CancellationToken = cancellationToken;
    }

    public TPayload Payload { get; }

    public CancellationToken CancellationToken { get; }

    public static ProcessRequest<TPayload> From(TPayload payload, CancellationToken cancellationToken = default) =>
        new(payload, cancellationToken);
}
