namespace LifeInsuranceCRM.Core.Abstractions.Auth;

public interface IActorTracker
{
    Guid? UserId { get; }

    string? UserEmail { get; }

    Guid? TenantId { get; }

    bool IsAuthenticated { get; }

    void SetActor(Guid userId, string? userEmail, Guid tenantId);

    void Clear();
}
